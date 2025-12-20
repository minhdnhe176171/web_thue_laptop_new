using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using web_chothue_laptop.Hubs;
using web_chothue_laptop.Services;

namespace web_chothue_laptop.Controllers
{
    public class AIChatController : Controller
    {
        private readonly AIChatService _aiChatService;
        private readonly RedisService _redisService;
        private readonly IHubContext<AIChatHub> _hubContext;

        public AIChatController(
            AIChatService aiChatService,
            RedisService redisService,
            IHubContext<AIChatHub> hubContext)
        {
            _aiChatService = aiChatService;
            _redisService = redisService;
            _hubContext = hubContext;
        }

        /// <summary>
        /// API endpoint để gửi tin nhắn cho AI chat
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Tin nhắn không được để trống" });
                }

                // Lấy hoặc tạo conversation context
                // Ưu tiên dùng UserId nếu đã đăng nhập (để persist giữa các trang)
                var userId = HttpContext.Session.GetString("UserId");
                string conversationId;
                
                // Nếu request có sessionId (từ client), parse nó
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    // sessionId từ client có format: "user:123" hoặc "session:xxx"
                    if (request.SessionId.StartsWith("user:"))
                    {
                        conversationId = $"ai_chat:{request.SessionId}";
                    }
                    else if (request.SessionId.StartsWith("session:"))
                    {
                        conversationId = $"ai_chat:{request.SessionId}";
                    }
                    else
                    {
                        // Fallback: dùng userId hoặc tạo mới
                        if (!string.IsNullOrEmpty(userId))
                        {
                            conversationId = $"ai_chat:user:{userId}";
                        }
                        else
                        {
                            conversationId = $"ai_chat:session:{request.SessionId}";
                        }
                    }
                }
                else
                {
                    // Nếu không có từ request, dùng userId hoặc sessionId mới
                    if (!string.IsNullOrEmpty(userId))
                    {
                        conversationId = $"ai_chat:user:{userId}";
                    }
                    else
                    {
                        var sessionId = HttpContext.Session.Id;
                        conversationId = $"ai_chat:session:{sessionId}";
                    }
                }
                
                // Lấy lịch sử hội thoại từ Redis
                var messages = await _redisService.GetAIChatMessagesAsync(conversationId);
                
                // Tạo hoặc lấy context
                var context = await _redisService.GetAIChatContextAsync(conversationId);
                
                if (context == null)
                {
                    // Tạo context mới từ lịch sử tin nhắn
                    var messageHistory = new List<string>();
                    foreach (var msg in messages)
                    {
                        messageHistory.Add(msg.Message);
                    }
                    context = new AIChatService.ConversationContext 
                    { 
                        Messages = messageHistory 
                    };
                }
                else
                {
                    // Đồng bộ lại messages từ Redis vào context
                    // Đảm bảo context.Messages có đầy đủ lịch sử
                    var existingMessages = context.Messages.ToList();
                    var redisMessages = messages.Select(m => m.Message).ToList();
                    
                    // Nếu Redis có nhiều tin nhắn hơn context, cập nhật context
                    if (redisMessages.Count > existingMessages.Count)
                    {
                        context.Messages = redisMessages;
                    }
                }

                // Xử lý tin nhắn với AI
                var response = await _aiChatService.ProcessMessageAsync(request.Message, context);

                // Lưu vào Redis SONG SONG (không đợi) để tăng tốc độ phản hồi
                // User sẽ nhận response ngay lập tức, Redis lưu ở background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Lưu tin nhắn người dùng
                        await _redisService.SaveAIChatMessageAsync(conversationId, new AIChatMessage
                        {
                            Message = request.Message,
                            SenderType = "user",
                            Timestamp = DateTime.UtcNow
                        });

                        // Lưu phản hồi AI
                        await _redisService.SaveAIChatMessageAsync(conversationId, new AIChatMessage
                        {
                            Message = response.Message,
                            SenderType = "assistant",
                            Timestamp = DateTime.UtcNow
                        });

                        // Cập nhật context
                        if (!context.Messages.Contains(request.Message))
                        {
                            context.Messages.Add(request.Message);
                        }
                        if (!context.Messages.Contains(response.Message))
                        {
                            context.Messages.Add(response.Message);
                        }
                        await _redisService.SaveAIChatContextAsync(conversationId, context);
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng không ảnh hưởng đến response
                        // Context có thể được xây dựng lại từ messages khi cần
                    }
                });

                // BỎ SignalR - Chỉ dùng AJAX response để tránh duplicate message
                // SignalR chỉ dùng cho chat giữa người với người, không cần cho AI chat

                return Ok(new
                {
                    message = response.Message,
                    recommendedLaptops = response.RecommendedLaptops?.Select(l => new
                    {
                        id = l.Id,
                        name = l.Name,
                        brand = l.Brand,
                        price = l.Price,
                        cpu = l.Cpu,
                        ram = l.Ram,
                        gpu = l.Gpu,
                        storage = l.Storage,
                        screenSize = l.ScreenSize,
                        os = l.Os
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Có lỗi xảy ra khi xử lý tin nhắn", details = ex.Message });
            }
        }

        /// <summary>
        /// Lấy lịch sử hội thoại
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetHistory(string? sessionId)
        {
            try
            {
                sessionId ??= HttpContext.Session.Id;
                
                // Tạo conversationId theo logic giống SendMessage
                string conversationId;
                var userId = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userId))
                {
                    conversationId = $"user:{userId}";
                }
                else
                {
                    conversationId = $"session:{sessionId}";
                }
                
                var messages = await _redisService.GetAIChatMessagesAsync(conversationId);
                
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Có lỗi xảy ra khi lấy lịch sử", details = ex.Message });
            }
        }

        /// <summary>
        /// Xóa lịch sử chat
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ClearHistory()
        {
            try
            {
                var sessionId = HttpContext.Session.Id;
                
                // Tạo conversationId theo logic giống SendMessage
                string conversationId;
                var userId = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userId))
                {
                    conversationId = $"user:{userId}";
                }
                else
                {
                    conversationId = $"session:{sessionId}";
                }
                
                await _redisService.ClearAIChatHistoryAsync(conversationId);
                
                return Ok(new { success = true, message = "Đã xóa lịch sử chat" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Có lỗi xảy ra khi xóa lịch sử", details = ex.Message });
            }
        }

        public class SendMessageRequest
        {
            public string Message { get; set; } = string.Empty;
            public string? SessionId { get; set; }
        }
    }
}


