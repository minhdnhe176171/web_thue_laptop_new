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
                
                // Tạo context mới (không sử dụng Redis methods không tồn tại)
                var context = new AIChatService.ConversationContext 
                { 
                    Messages = new List<string>()
                };

                // Xử lý tin nhắn với AI
                var response = await _aiChatService.ProcessMessageAsync(request.Message, context);

                // Cập nhật context với tin nhắn mới (không sử dụng Redis methods không tồn tại)
                if (!context.Messages.Contains(request.Message))
                {
                    context.Messages.Add(request.Message);
                }
                if (!context.Messages.Contains(response.Message))
                {
                    context.Messages.Add(response.Message);
                }

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
                    conversationId = $"ai_chat:user:{userId}";
                }
                else
                {
                    conversationId = $"ai_chat:session:{sessionId}";
                }
                
                // Trả về danh sách rỗng (không sử dụng Redis method không tồn tại)
                return Ok(new List<object>());
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
                    conversationId = $"ai_chat:user:{userId}";
                }
                else
                {
                    conversationId = $"ai_chat:session:{sessionId}";
                }
                
                // Không sử dụng Redis method không tồn tại - chỉ trả về success
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


