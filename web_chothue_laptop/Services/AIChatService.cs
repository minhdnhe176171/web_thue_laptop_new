using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using web_chothue_laptop.Services;

namespace web_chothue_laptop.Services
{
    public class AIChatService
    {
        private readonly RagService _ragService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AIChatService> _logger;
        private readonly string? _openAiApiKey;
        private readonly string? _openAiBaseUrl;
        private readonly string? _shopName;

        // System prompt template
        private const string SystemPromptTemplate = @"Bạn là một chuyên gia tư vấn cho thuê laptop chuyên nghiệp của cửa hàng {SHOP_NAME}. Nhiệm vụ của bạn là dựa trên danh sách sản phẩm được cung cấp từ database để tư vấn cho khách hàng.

NGUYÊN TẮC XỬ LÝ CÁC LOẠI CÂU HỎI:

A. MÁY KHÔNG CÓ TRONG DANH SÁCH:
Khi khách hỏi máy cụ thể không có trong danh sách (ví dụ: Macbook M3, Thinkpad X1 Carbon, Intel Gen 14):
1. THỪA NHẬN: ""Hiện tại shop chưa có [tên máy] trong kho, nhưng..."" 
2. PHÂN TÍCH ĐẶC ĐIỂM: Nhận diện đặc điểm của máy khách cần (Macbook → mỏng nhẹ, premium, chip M-series → hiệu năng cao; Thinkpad → business, bền bỉ; Intel Gen 14 → CPU mới nhất)
3. GỢI Ý THAY THẾ: Đề xuất laptop CÓ SẴN có đặc điểm tương đương:
   - Macbook M3 không có → Gợi ý: Dell XPS (mỏng nhẹ, premium, hiệu năng cao) hoặc laptop mỏng nhẹ Intel i7/i5 gen 12-13
   - Thinkpad X1 Carbon không có → Gợi ý: Laptop business bền bỉ, bàn phím tốt (Dell Latitude, HP EliteBook nếu có)
   - Intel Gen 14 không có → Gợi ý: Intel Gen 13 hoặc 12, hoặc AMD Ryzen 7000 series
4. NHẤN MẠNH LỢI ÍCH: Giải thích tại sao máy thay thế phù hợp (cấu hình tương đương, giá tốt hơn, có sẵn ngay)

B. PHỤ KIỆN & DỊCH VỤ:
Khi khách hỏi về phụ kiện (chuột, túi chống sốc, màn hình rời) hoặc dịch vụ (hỗ trợ khi máy hỏng):
1. PHỤ KIỆN: 
   - ""Chuột, túi chống sốc: Shop có thể cung cấp kèm theo khi thuê (vui lòng hỏi nhân viên khi đặt hàng để biết chi tiết)""
   - ""Màn hình rời: Hiện shop chủ yếu cho thuê laptop. Bạn có thể kết nối laptop với màn hình ngoài qua cổng HDMI/USB-C""
2. DỊCH VỤ HỖ TRỢ:
   - ""Nếu máy hỏng khi đang thuê: Shop có chính sách đổi máy ngay trong vòng 24h, hỗ trợ kỹ thuật 24/7, bảo hành đầy đủ""
   - ""Tất cả laptop đều được kiểm tra kỹ trước khi cho thuê, đảm bảo tình trạng tốt""

F. CHÍNH SÁCH & BẢO HÀNH:
Khi khách hỏi về chính sách bảo hành, thủ tục thuê, hoặc xử lý sự cố:
1. MÁY HỎNG KHI ĐANG THUÊ:
   → TRẢ LỜI: ""Bạn hoàn toàn yên tâm. Nếu máy gặp sự cố, hãy truy cập chức năng báo lỗi để gửi lỗi cho kỹ thuật viên, chúng tôi sẽ mang một máy khác đến cho bạn. Còn về sự cố chúng tôi sẽ xác định và đưa ra phương pháp giải quyết sau, công việc của bạn vẫn sẽ được ưu tiên.""
2. THỦ TỤC THUÊ CHO SINH VIÊN:
   → TRẢ LỜI: ""Chỉ cần CCCD và thẻ sinh viên kèm ảnh chụp chính chủ là có thể làm thủ tục. Hóa đơn sẽ được thanh toán và giao máy tại quầy ngay tức thì.""
3. CỌC TIỀN:
   → TRẢ LỜI: ""Sinh viên không cần cọc tiền, chỉ cần CCCD và thẻ sinh viên. Khách hàng khác vui lòng hỏi nhân viên để biết chi tiết về chính sách cọc.""
4. BẢO HÀNH:
   → TRẢ LỜI: ""Tất cả laptop đều được bảo hành đầy đủ trong thời gian thuê. Nếu có sự cố, shop sẽ hỗ trợ sửa chữa hoặc đổi máy ngay.""

C. NHU CẦU CỤ THỂ THEO NGÀNH NGHỀ:
- Marketing/Content Creator: ""mỏng nhẹ + pin trâu"" → Laptop Ultrabook, Intel i5/i7 gen 12-13, RAM 8-16GB, pin 50Wh+
- Gaming: ""tản nhiệt tốt"" → Laptop gaming có hệ thống tản nhiệt tốt, nhiều quạt, có GPU RTX/GTX
- Đồ họa: ""màn hình tốt + GPU mạnh"" → Laptop có GPU RTX, màn hình IPS/FHD, RAM 16GB+
- Văn phòng: ""pin lâu + nhẹ"" → Laptop tiết kiệm pin, i5/i3 gen 12+, RAM 8GB

D. SO SÁNH CẤU HÌNH KỸ THUẬT:
Khi so sánh (ví dụ: ""Asus TUF vs HP Victus, tản nhiệt""):
1. SO SÁNH ĐẶC ĐIỂM: Tản nhiệt, thiết kế, giá, phù hợp với nhu cầu nào
2. ĐƯA RA KHUYẾN NGHỊ dựa trên nhu cầu cụ thể (gaming, đồ họa, văn phòng)
3. Nếu không có cả 2 máy trong danh sách → So sánh các laptop gaming TƯƠNG ĐƯƠNG có sẵn

E. CÁC CÂU HỎI KHÁC:
- GIÁ: Liệt kê laptop trong khoảng giá, nếu không có → gợi ý laptop gần nhất
- THÔNG SỐ: Liệt kê laptop có thông số đó
- TỔNG QUÁT: Hỏi thêm để hiểu rõ nhu cầu, sau đó đề xuất

NGUYÊN TẮC TRẢ LỜI:
- TỰ NHIÊN, THÂN THIỆN, KHÔNG CỨNG NHẮC: Trả lời như một người tư vấn thật, không phải bot
- KHI KHÔNG CÓ MÁY: Đừng chỉ nói ""không có"", hãy gợi ý máy tương đương và giải thích tại sao phù hợp
- KHI THIẾU THÔNG TIN: Hỏi thêm một cách tự nhiên, không liệt kê câu hỏi mẫu cứng nhắc
- FORMAT: Giá tiền VNĐ (ví dụ: 5.000.000 VNĐ/tháng), trả lời ngắn gọn nhưng đầy đủ thông tin
- CHỈ ĐỀ XUẤT laptop CÓ SẴN trong danh sách được cung cấp";

        public AIChatService(
            RagService ragService,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AIChatService> logger)
        {
            _ragService = ragService;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            _openAiApiKey = _configuration["OpenAI:ApiKey"];
            _openAiBaseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
            _shopName = _configuration["ShopSettings:Name"] ?? "[Tên Shop]";

            if (!string.IsNullOrEmpty(_openAiApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
            }
        }

        public class ChatResponse
        {
            public string Message { get; set; } = string.Empty;
            public List<RagService.LaptopInfo>? RecommendedLaptops { get; set; }
        }

        public class ConversationContext
        {
            public List<string> Messages { get; set; } = new();
            public RagService.CustomerNeeds? CurrentNeeds { get; set; }
            public string? UsageType { get; set; }
            public decimal? Budget { get; set; }
        }

        /// <summary>
        /// Xử lý tin nhắn từ khách hàng và trả về phản hồi AI
        /// </summary>
        public async Task<ChatResponse> ProcessMessageAsync(string userMessage, ConversationContext context)
        {
            try
            {
                // Đảm bảo context không null
                if (context == null)
                {
                    context = new ConversationContext();
                }
                
                // Đếm số tin nhắn USER trước khi thêm tin nhắn hiện tại
                // (Loại bỏ tin nhắn hiện tại nếu đã có để tránh duplicate)
                var messagesWithoutCurrent = context.Messages.Where(m => m != userMessage).ToList();
                
                // Đếm số tin nhắn user (mỗi 2 tin nhắn là 1 cặp user-assistant)
                // Hoặc đơn giản là đếm số tin nhắn trước khi thêm tin nhắn hiện tại
                int userMessageCount = 0;
                for (int i = 0; i < messagesWithoutCurrent.Count; i += 2)
                {
                    userMessageCount++; // Mỗi tin nhắn user
                }
                
                // Thêm tin nhắn vào lịch sử (nếu chưa có)
                if (!context.Messages.Contains(userMessage))
                {
                    context.Messages.Add(userMessage);
                }

                // Phân tích nhu cầu từ tin nhắn
                var needs = _ragService.AnalyzeCustomerNeeds(userMessage, context.Messages);
                
                // Cập nhật context
                if (!string.IsNullOrEmpty(needs.UsageType))
                    context.UsageType = needs.UsageType;
                if (needs.MinBudget.HasValue || needs.MaxBudget.HasValue)
                {
                    context.Budget = needs.MaxBudget ?? needs.MinBudget;
                }
                context.CurrentNeeds = needs;

                // Kiểm tra xem có nhu cầu rõ ràng không (bao gồm cả yêu cầu đặc biệt)
                bool hasValidNeeds = !string.IsNullOrEmpty(needs.UsageType) || 
                                     needs.MinBudget.HasValue || needs.MaxBudget.HasValue ||
                                     needs.CpuRequirement != null || needs.RamRequirement != null || needs.GpuRequirement != null ||
                                     needs.RequiresThinLight || needs.RequiresLongBattery || needs.RequiresGoodCooling;
                
                // Kiểm tra xem có phải câu hỏi về giá hoặc so sánh không
                var userMessageLower = userMessage.ToLower();
                bool isPriceQuery = userMessageLower.Contains("giá") || userMessageLower.Contains("bao nhiêu") ||
                                    userMessageLower.Contains("dưới") || userMessageLower.Contains("trên") ||
                                    userMessageLower.Contains("khoảng") || userMessageLower.Contains("tầm") ||
                                    needs.MinBudget.HasValue || needs.MaxBudget.HasValue;
                bool isComparisonQuery = userMessageLower.Contains("so sánh") || userMessageLower.Contains("khác nhau") ||
                                         userMessageLower.Contains("mạnh hơn") || userMessageLower.Contains("yếu hơn") ||
                                         userMessageLower.Contains("và") && (userMessageLower.Contains("laptop") || userMessageLower.Contains("máy"));
                bool isSpecQuery = userMessageLower.Contains("rtx") || userMessageLower.Contains("gtx") ||
                                   userMessageLower.Contains("i3") || userMessageLower.Contains("i5") || 
                                   userMessageLower.Contains("i7") || userMessageLower.Contains("i9") ||
                                   userMessageLower.Contains("ryzen") || userMessageLower.Contains("amd") ||
                                   userMessageLower.Contains("ram") || userMessageLower.Contains("gpu") ||
                                   userMessageLower.Contains("cpu") || userMessageLower.Contains("card đồ họa");
                bool isFeatureQuery = userMessageLower.Contains("mỏng nhẹ") || userMessageLower.Contains("mỏng") || userMessageLower.Contains("nhẹ") ||
                                     userMessageLower.Contains("pin trâu") || userMessageLower.Contains("pin lâu") ||
                                     userMessageLower.Contains("tản nhiệt") || userMessageLower.Contains("ultrabook") ||
                                     needs.RequiresThinLight || needs.RequiresLongBattery || needs.RequiresGoodCooling;
                
                // Luôn query laptop nếu có câu hỏi về giá, so sánh, thông số, hoặc tính năng đặc biệt
                bool shouldQueryLaptops = hasValidNeeds || isPriceQuery || isComparisonQuery || isSpecQuery || isFeatureQuery;
                
                // Chỉ tìm laptop khi cần thiết
                List<RagService.LaptopInfo> matchingLaptops = new();
                if (shouldQueryLaptops)
                {
                    if (hasValidNeeds)
                    {
                        // Lấy danh sách laptop phù hợp với nhu cầu
                        matchingLaptops = await _ragService.FindMatchingLaptopsAsync(needs);
                    }
                    else if (isPriceQuery || isSpecQuery || isComparisonQuery)
                    {
                        // Nếu chỉ có câu hỏi về giá/thông số/so sánh mà không có usage type,
                        // vẫn query tất cả laptop có sẵn để AI có thể trả lời
                        matchingLaptops = await _ragService.GetAvailableLaptopsAsync();
                    }
                    
                    // Nếu không tìm thấy laptop phù hợp, lấy tất cả laptop có sẵn
                    if (!matchingLaptops.Any())
                    {
                        matchingLaptops = await _ragService.GetAvailableLaptopsAsync();
                    }
                }

                // Với câu hỏi so sánh hoặc về giá, có thể cần nhiều laptop hơn để trả lời
                // Giảm số lượng để tăng tốc độ phản hồi
                int maxLaptopsForContext = (isComparisonQuery || isPriceQuery) ? 12 : 8;
                var laptopsForContext = matchingLaptops.Take(maxLaptopsForContext).ToList();
                
                // Recommended laptops: 3 cho câu hỏi thông thường, nhiều hơn cho so sánh
                int maxRecommended = isComparisonQuery ? 4 : 3;
                var recommendedLaptops = matchingLaptops.Take(maxRecommended).ToList();

                // Tạo context cho AI
                var systemPrompt = SystemPromptTemplate.Replace("{SHOP_NAME}", _shopName);
                
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };
                
                // Thêm danh sách laptop nếu có laptop và cần thiết
                if (shouldQueryLaptops && laptopsForContext.Any())
                {
                    var laptopContext = BuildLaptopContext(laptopsForContext);
                    
                    // Thêm hướng dẫn cho AI dựa trên loại câu hỏi
                    string instruction = "";
                    if (isPriceQuery)
                    {
                        instruction = "KHÁCH HÀNG ĐANG HỎI VỀ GIÁ. Hãy liệt kê các laptop phù hợp với khoảng giá họ yêu cầu từ danh sách trên. Nếu không có laptop nào trong khoảng giá đó, hãy gợi ý laptop gần nhất.\n\n";
                    }
                    else if (isComparisonQuery)
                    {
                        instruction = "KHÁCH HÀNG ĐANG HỎI SO SÁNH. Hãy so sánh chi tiết các laptop được đề cập, bao gồm: CPU, RAM, GPU, giá, phù hợp với nhu cầu nào. Đưa ra khuyến nghị dựa trên nhu cầu của khách.\n\n";
                    }
                    else if (isSpecQuery)
                    {
                        instruction = "KHÁCH HÀNG ĐANG HỎI VỀ THÔNG SỐ KỸ THUẬT. Hãy liệt kê các laptop có thông số họ yêu cầu từ danh sách trên, kèm giá và cấu hình đầy đủ.\n\n";
                    }
                    else if (hasValidNeeds)
                    {
                        instruction = "KHÁCH HÀNG ĐÃ CUNG CẤP NHU CẦU. Hãy đề xuất laptop phù hợp từ danh sách trên.\n\n";
                    }
                    
                    messages.Add(new { role = "system", content = $"{instruction}DANH SÁCH LAPTOP CÓ SẴN:\n{laptopContext}" });
                }
                else if (!shouldQueryLaptops)
                {
                    // Nếu chưa có câu hỏi rõ ràng về laptop, thông báo
                    messages.Add(new { role = "system", content = "Lưu ý: Bạn chưa nhận được thông tin nhu cầu cụ thể từ khách hàng. Hãy hỏi họ về mục đích sử dụng (văn phòng, đồ họa, gaming, lập trình) và ngân sách trước khi đề xuất laptop." });
                }
                else if (shouldQueryLaptops && !laptopsForContext.Any())
                {
                    messages.Add(new { role = "system", content = "Lưu ý: Hiện tại không có laptop nào phù hợp với yêu cầu của khách hàng. Hãy thông báo rõ ràng và gợi ý các lựa chọn thay thế hoặc hỏi thêm thông tin." });
                }

                // Thêm lịch sử hội thoại (chỉ lấy 6 tin nhắn gần nhất để giảm context và tăng tốc)
                // Sử dụng messagesWithoutCurrent đã tính ở trên
                var messagesToProcess = messagesWithoutCurrent.ToList();
                
                // Chỉ lấy 6 tin nhắn gần nhất (3 cặp user-assistant)
                var recentMessages = messagesToProcess.Skip(Math.Max(0, messagesToProcess.Count - 6)).ToList();
                
                // Xây dựng conversation history theo cặp user-assistant
                int messageIndex = 0;
                while (messageIndex < recentMessages.Count)
                {
                    // Tin nhắn user
                    if (messageIndex < recentMessages.Count)
                    {
                        messages.Add(new { role = "user", content = recentMessages[messageIndex] });
                        messageIndex++;
                    }
                    
                    // Tin nhắn assistant (nếu có)
                    if (messageIndex < recentMessages.Count)
                    {
                        messages.Add(new { role = "assistant", content = recentMessages[messageIndex] });
                        messageIndex++;
                    }
                }

                // Thêm tin nhắn người dùng hiện tại
                messages.Add(new { role = "user", content = userMessage });

                // Gọi OpenAI API
                if (string.IsNullOrEmpty(_openAiApiKey))
                {
                    // Fallback: Trả về response đơn giản nếu không có API key
                    // Sử dụng userMessageCount + 1 (tin nhắn hiện tại) để tính
                    // +1 vì đang thêm tin nhắn hiện tại
                    // Chỉ truyền recommendedLaptops nếu có nhu cầu rõ ràng
                    var laptopsToRecommend = hasValidNeeds ? recommendedLaptops : new List<RagService.LaptopInfo>();
                    return GenerateFallbackResponse(userMessage, laptopsToRecommend, needs, userMessageCount + 1);
                }

                var response = await CallOpenAIAsync(messages);
                
                // Thêm tin nhắn vào context sau khi đã xử lý xong (tránh duplicate)
                if (!context.Messages.Contains(userMessage))
                {
                    context.Messages.Add(userMessage);
                }
                
                // Recommend laptop nếu có nhu cầu rõ ràng, hoặc có câu hỏi về giá/so sánh/thông số
                bool shouldRecommend = hasValidNeeds || isPriceQuery || isComparisonQuery || isSpecQuery;
                return new ChatResponse
                {
                    Message = response,
                    RecommendedLaptops = (shouldRecommend && recommendedLaptops.Any()) ? recommendedLaptops : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI chat message");
                
                // Fallback response
                var matchingLaptops = await _ragService.FindMatchingLaptopsAsync(context.CurrentNeeds ?? new RagService.CustomerNeeds());
                var recommendedLaptops = matchingLaptops.Take(3).ToList();
                
                int messageCount = context?.Messages?.Count ?? 0;
                return GenerateFallbackResponse(userMessage, recommendedLaptops, context.CurrentNeeds ?? new RagService.CustomerNeeds(), messageCount + 1);
            }
        }

        private string BuildLaptopContext(List<RagService.LaptopInfo> laptops)
        {
            if (!laptops.Any())
                return "Hiện tại không có laptop nào có sẵn để cho thuê.";

            var sb = new StringBuilder();
            sb.AppendLine($"Tổng cộng có {laptops.Count} laptop có sẵn:\n");

            foreach (var laptop in laptops)
            {
                sb.AppendLine($"- ID: {laptop.Id}");
                sb.AppendLine($"  Tên: {laptop.Brand} {laptop.Name}");
                sb.AppendLine($"  CPU: {laptop.Cpu}");
                sb.AppendLine($"  RAM: {laptop.Ram}");
                sb.AppendLine($"  Card đồ họa: {laptop.Gpu}");
                sb.AppendLine($"  Ổ cứng: {laptop.Storage}");
                sb.AppendLine($"  Màn hình: {laptop.ScreenSize}");
                sb.AppendLine($"  Hệ điều hành: {laptop.Os}");
                sb.AppendLine($"  Giá thuê: {laptop.Price:N0} VNĐ/tháng");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private async Task<string> CallOpenAIAsync(List<object> messages)
        {
            try
            {
                var requestBody = new
                {
                    model = _configuration["OpenAI:Model"] ?? "gpt-3.5-turbo",
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 600  // Giảm từ 1000 xuống 600 để tăng tốc độ phản hồi
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_openAiBaseUrl}/chat/completions", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var responseJson = JsonDocument.Parse(responseBody);

                return responseJson.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Xin lỗi, tôi không thể xử lý yêu cầu của bạn lúc này.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
                throw;
            }
        }

        private ChatResponse GenerateFallbackResponse(
            string userMessage, 
            List<RagService.LaptopInfo> laptops, 
            RagService.CustomerNeeds needs,
            int messageCount = 0)
        {
            var message = userMessage.ToLower();

            // Chỉ chào khi thực sự là tin nhắn đầu tiên VÀ là lời chào
            // messageCount ở đây là số tin nhắn USER (đã được tính đúng ở trên)
            bool isGreeting = message.Contains("chào") || message.Contains("xin chào") || message.Contains("hello") || message.Contains("hi");
            bool isFirstMessage = messageCount == 1; // Chỉ khi là tin nhắn user đầu tiên (== 1, không phải <= 1)
            
            // Kiểm tra xem tin nhắn có liên quan đến laptop không (mở rộng)
            bool isLaptopRelated = message.Contains("laptop") || message.Contains("máy tính") || 
                                   message.Contains("gaming") || message.Contains("chơi game") || 
                                   message.Contains("đồ họa") || message.Contains("design") ||
                                   message.Contains("lập trình") || message.Contains("programming") ||
                                   message.Contains("văn phòng") || message.Contains("office") ||
                                   message.Contains("học") || message.Contains("học tập") ||
                                   message.Contains("cpu") || message.Contains("ram") || message.Contains("gpu") ||
                                   message.Contains("thuê") || message.Contains("cho thuê") ||
                                   message.Contains("giá") || message.Contains("bao nhiêu") ||
                                   message.Contains("so sánh") || message.Contains("khác nhau") ||
                                   message.Contains("rtx") || message.Contains("gtx") ||
                                   message.Contains("i3") || message.Contains("i5") || message.Contains("i7") || message.Contains("i9") ||
                                   message.Contains("ryzen") || message.Contains("amd") ||
                                   message.Contains("card đồ họa") || message.Contains("graphics") ||
                                   message.Contains("macbook") || message.Contains("thinkpad") || message.Contains("dell") ||
                                   message.Contains("hp") || message.Contains("asus") || message.Contains("lenovo") ||
                                   message.Contains("tản nhiệt") || message.Contains("pin") || message.Contains("mỏng nhẹ") ||
                                   message.Contains("marketing") || message.Contains("content") ||
                                   message.Contains("phụ kiện") || message.Contains("chuột") || message.Contains("túi chống sốc") ||
                                   message.Contains("màn hình rời") || message.Contains("dịch vụ") || message.Contains("hỗ trợ") ||
                                   message.Contains("máy hỏng") || message.Contains("bảo hành") || message.Contains("sự cố") ||
                                   message.Contains("thủ tục") || message.Contains("cọc tiền") || message.Contains("cccd") ||
                                   message.Contains("thẻ sinh viên") || message.Contains("chính sách") ||
                                   needs.UsageType != null || needs.MinBudget.HasValue || needs.MaxBudget.HasValue ||
                                   needs.CpuRequirement != null || needs.RamRequirement != null || needs.GpuRequirement != null;
            
            // Nếu là lời chào đầu tiên, trả về lời chào
            if (isFirstMessage && isGreeting)
            {
                return new ChatResponse
                {
                    Message = "Xin chào! Tôi là trợ lý tư vấn cho thuê laptop. Bạn đang tìm laptop cho mục đích sử dụng nào? (văn phòng, đồ họa, gaming, hay lập trình?)",
                    RecommendedLaptops = null
                };
            }
            
            // Nếu không liên quan đến laptop và không có thông tin nhu cầu
            if (!isLaptopRelated && string.IsNullOrEmpty(needs.UsageType) && 
                !needs.MinBudget.HasValue && !needs.MaxBudget.HasValue)
            {
                // Trả lời thân thiện và hỏi về laptop
                var responseMessage = new StringBuilder();
                
                if (isFirstMessage)
                {
                    responseMessage.Append("Xin chào! ");
                }
                
                responseMessage.Append("Tôi là trợ lý tư vấn cho thuê laptop. ");
                responseMessage.Append("Tôi có thể giúp bạn tìm laptop phù hợp với nhu cầu. ");
                responseMessage.Append("Bạn đang tìm laptop cho mục đích sử dụng nào? (văn phòng, đồ họa, gaming, hay lập trình?)");
                
                return new ChatResponse
                {
                    Message = responseMessage.ToString(),
                    RecommendedLaptops = null
                };
            }
            
            // Chỉ recommend laptop khi có thông tin nhu cầu rõ ràng (bao gồm yêu cầu tính năng)
            bool hasValidNeeds = !string.IsNullOrEmpty(needs.UsageType) || 
                                 needs.MinBudget.HasValue || needs.MaxBudget.HasValue ||
                                 needs.CpuRequirement != null || needs.RamRequirement != null || needs.GpuRequirement != null ||
                                 needs.RequiresThinLight || needs.RequiresLongBattery || needs.RequiresGoodCooling;

            // Nếu có laptop phù hợp VÀ có nhu cầu rõ ràng
            if (laptops.Any() && hasValidNeeds)
            {
                var sb = new StringBuilder();
                
                if (!string.IsNullOrEmpty(needs.UsageType))
                {
                    sb.AppendLine($"Dựa trên nhu cầu {needs.UsageType} của bạn, tôi xin đề xuất {Math.Min(3, laptops.Count)} laptop phù hợp:\n");
                }
                else
                {
                    sb.AppendLine($"Dưới đây là {Math.Min(3, laptops.Count)} laptop phù hợp:\n");
                }

                int index = 1;
                foreach (var laptop in laptops.Take(3))
                {
                    sb.AppendLine($"{index}. {laptop.Brand} {laptop.Name}");
                    sb.AppendLine($"   - Cấu hình: CPU {laptop.Cpu}, RAM {laptop.Ram}, {laptop.Gpu}");
                    sb.AppendLine($"   - Giá thuê: {laptop.Price:N0} VNĐ/tháng");
                    sb.AppendLine($"   - Màn hình: {laptop.ScreenSize}, Ổ cứng: {laptop.Storage}");
                    sb.AppendLine();
                    index++;
                }

                sb.AppendLine("Tất cả laptop đều ở tình trạng tốt và được bảo hành đầy đủ. Bạn có muốn biết thêm chi tiết về laptop nào không?");

                return new ChatResponse
                {
                    Message = sb.ToString(),
                    RecommendedLaptops = laptops.Take(3).ToList()
                };
            }

            // Nếu không có nhu cầu rõ ràng, phân tích câu hỏi và trả lời tự nhiên hơn
            if (!hasValidNeeds)
            {
                // Kiểm tra các loại câu hỏi đặc biệt
                bool isMacbookQuery = message.Contains("macbook") || message.Contains("mac book") || message.Contains("m3") || message.Contains("m2") || message.Contains("m1");
                bool isThinkpadQuery = message.Contains("thinkpad") || message.Contains("think pad") || message.Contains("x1 carbon");
                bool isAccessoryQuery = message.Contains("chuột") || message.Contains("túi chống sốc") || message.Contains("phụ kiện") || 
                                       message.Contains("màn hình rời") || message.Contains("màn hình để làm đồ họa");
                bool isServiceQuery = message.Contains("máy hỏng") || message.Contains("hỗ trợ") || message.Contains("dịch vụ") || 
                                     message.Contains("bảo hành") || message.Contains("đổi máy") ||
                                     message.Contains("sự cố") || message.Contains("báo lỗi") || message.Contains("kỹ thuật viên");
                bool isPolicyQuery = message.Contains("thủ tục") || message.Contains("cọc tiền") || message.Contains("cọc") ||
                                    message.Contains("cccd") || message.Contains("thẻ sinh viên") || message.Contains("sinh viên") ||
                                    message.Contains("giấy tờ") || message.Contains("hóa đơn") || message.Contains("thanh toán") ||
                                    message.Contains("giao máy") || message.Contains("quầy");
                bool isGenQuery = message.Contains("gen 14") || message.Contains("thế hệ 14") || message.Contains("intel gen");
                
                var responseBuilder = new StringBuilder();
                
                // Xử lý câu hỏi về máy không có trong DB
                if (isMacbookQuery)
                {
                    responseBuilder.AppendLine("Hiện tại shop chưa có Macbook M3 trong kho. Tuy nhiên, shop có các laptop premium mỏng nhẹ với hiệu năng cao tương đương như:");
                    if (laptops.Any())
                    {
                        foreach (var laptop in laptops.Take(3).Where(l => l.Cpu.ToLower().Contains("i7") || l.Cpu.ToLower().Contains("i5")))
                        {
                            responseBuilder.AppendLine($"- {laptop.Brand} {laptop.Name}: {laptop.Cpu}, RAM {laptop.Ram}, Giá {laptop.Price:N0} VNĐ/tháng");
                        }
                    }
                    responseBuilder.AppendLine("\nCác laptop này có cấu hình tương đương, mỏng nhẹ và phù hợp cho công việc chuyên nghiệp. Bạn muốn tôi tư vấn chi tiết về laptop nào không?");
                    
                    return new ChatResponse
                    {
                        Message = responseBuilder.ToString(),
                        RecommendedLaptops = laptops.Take(3).ToList()
                    };
                }
                
                if (isThinkpadQuery)
                {
                    responseBuilder.AppendLine("Shop hiện chưa có Thinkpad X1 Carbon. Thay vào đó, shop có các laptop business bền bỉ, bàn phím tốt phù hợp cho công việc văn phòng:");
                    if (laptops.Any())
                    {
                        foreach (var laptop in laptops.Take(3))
                        {
                            responseBuilder.AppendLine($"- {laptop.Brand} {laptop.Name}: {laptop.Cpu}, RAM {laptop.Ram}, Giá {laptop.Price:N0} VNĐ/tháng");
                        }
                    }
                    responseBuilder.AppendLine("\nCác laptop này đều được kiểm tra kỹ, đảm bảo chất lượng và phù hợp cho công việc chuyên nghiệp. Bạn có thể cho tôi biết thêm về nhu cầu sử dụng để tôi tư vấn chính xác hơn không?");
                    
                    return new ChatResponse
                    {
                        Message = responseBuilder.ToString(),
                        RecommendedLaptops = laptops.Take(3).ToList()
                    };
                }
                
                if (isGenQuery)
                {
                    responseBuilder.AppendLine("Shop hiện chưa có laptop dùng Intel Gen 14. Tuy nhiên, shop có các laptop với CPU thế hệ gần đây (Gen 12-13) hoặc AMD Ryzen mới nhất với hiệu năng tương đương:");
                    if (laptops.Any())
                    {
                        foreach (var laptop in laptops.Take(3))
                        {
                            responseBuilder.AppendLine($"- {laptop.Brand} {laptop.Name}: {laptop.Cpu}, RAM {laptop.Ram}, Giá {laptop.Price:N0} VNĐ/tháng");
                        }
                    }
                    responseBuilder.AppendLine("\nBạn có thể cho tôi biết thêm về mục đích sử dụng để tôi đề xuất laptop phù hợp nhất không?");
                    
                    return new ChatResponse
                    {
                        Message = responseBuilder.ToString(),
                        RecommendedLaptops = laptops.Take(3).ToList()
                    };
                }
                
                // Xử lý câu hỏi về phụ kiện
                if (isAccessoryQuery)
                {
                    if (message.Contains("chuột") || message.Contains("túi chống sốc"))
                    {
                        responseBuilder.AppendLine("Shop có thể cung cấp chuột và túi chống sốc kèm theo khi thuê laptop. Vui lòng hỏi nhân viên khi đặt hàng để biết chi tiết về phụ kiện và chi phí (nếu có).");
                    }
                    else if (message.Contains("màn hình rời"))
                    {
                        responseBuilder.AppendLine("Hiện shop chủ yếu cho thuê laptop. Bạn có thể kết nối laptop với màn hình ngoài qua cổng HDMI hoặc USB-C (tùy laptop). Nếu cần tư vấn về laptop phù hợp để kết nối màn hình ngoài cho công việc đồ họa, tôi có thể giúp bạn chọn laptop có GPU tốt và cổng kết nối phù hợp.");
                    }
                    responseBuilder.AppendLine("\nBạn đang tìm laptop cho mục đích sử dụng nào? Tôi có thể tư vấn laptop phù hợp cho bạn.");
                    
                    return new ChatResponse
                    {
                        Message = responseBuilder.ToString(),
                        RecommendedLaptops = null
                    };
                }
                
                // Xử lý câu hỏi về dịch vụ và bảo hành
                if (isServiceQuery)
                {
                    if (message.Contains("máy hỏng") || message.Contains("sự cố") || message.Contains("bị lỗi") || 
                        message.Contains("đang dùng mà máy") || message.Contains("dùng mà máy bị"))
                    {
                        // Câu hỏi cụ thể về máy hỏng khi đang thuê
                        responseBuilder.AppendLine("Bạn hoàn toàn yên tâm. Nếu máy gặp sự cố, hãy truy cập chức năng báo lỗi để gửi lỗi cho kỹ thuật viên, chúng tôi sẽ mang một máy khác đến cho bạn. Còn về sự cố chúng tôi sẽ xác định và đưa ra phương pháp giải quyết sau, công việc của bạn vẫn sẽ được ưu tiên.");
                    }
                    else
                    {
                        // Câu hỏi chung về dịch vụ
                        responseBuilder.AppendLine("Shop có chính sách hỗ trợ khách hàng tốt:");
                        responseBuilder.AppendLine("- Nếu máy hỏng trong lúc thuê: Shop sẽ đổi máy ngay trong vòng 24h");
                        responseBuilder.AppendLine("- Hỗ trợ kỹ thuật 24/7 qua hotline");
                        responseBuilder.AppendLine("- Tất cả laptop đều được kiểm tra kỹ trước khi cho thuê, đảm bảo tình trạng tốt");
                        responseBuilder.AppendLine("- Bảo hành đầy đủ trong thời gian thuê");
                    }
                    responseBuilder.AppendLine("\nBạn có đang tìm laptop để thuê không? Tôi có thể tư vấn laptop phù hợp với nhu cầu của bạn.");
                    
                    return new ChatResponse
                    {
                        Message = responseBuilder.ToString(),
                        RecommendedLaptops = null
                    };
                }
                
                // Xử lý câu hỏi về chính sách và thủ tục thuê
                if (isPolicyQuery)
                {
                    if (message.Contains("sinh viên") || message.Contains("thẻ sinh viên"))
                    {
                        if (message.Contains("cọc") || message.Contains("cọc tiền"))
                        {
                            // Thủ tục thuê cho sinh viên - câu hỏi về cọc
                            responseBuilder.AppendLine("Chỉ cần CCCD và thẻ sinh viên kèm ảnh chụp chính chủ là có thể làm thủ tục. Hóa đơn sẽ được thanh toán và giao máy tại quầy ngay tức thì.");
                            responseBuilder.AppendLine("\nSinh viên không cần cọc tiền, chỉ cần xuất trình giấy tờ tùy thân và thẻ sinh viên.");
                        }
                        else
                        {
                            // Thủ tục thuê cho sinh viên - câu hỏi chung
                            responseBuilder.AppendLine("Chỉ cần CCCD và thẻ sinh viên kèm ảnh chụp chính chủ là có thể làm thủ tục. Hóa đơn sẽ được thanh toán và giao máy tại quầy ngay tức thì.");
                        }
                    }
                    else if (message.Contains("cọc") || message.Contains("cọc tiền"))
                    {
                        // Câu hỏi về cọc tiền (không phải sinh viên)
                        responseBuilder.AppendLine("Về chính sách cọc tiền, vui lòng liên hệ trực tiếp với nhân viên tại quầy để biết chi tiết. Sinh viên thì không cần cọc, chỉ cần CCCD và thẻ sinh viên.");
                    }
                    else if (message.Contains("thủ tục") || message.Contains("giấy tờ"))
                    {
                        // Câu hỏi về thủ tục thuê chung
                        responseBuilder.AppendLine("Thủ tục thuê laptop rất đơn giản:");
                        responseBuilder.AppendLine("- Sinh viên: Chỉ cần CCCD và thẻ sinh viên kèm ảnh chụp chính chủ");
                        responseBuilder.AppendLine("- Khách hàng khác: Vui lòng liên hệ quầy để biết chi tiết về giấy tờ cần thiết");
                        responseBuilder.AppendLine("- Hóa đơn sẽ được thanh toán và giao máy tại quầy ngay tức thì");
                    }
                    else
                    {
                        // Câu hỏi chung về chính sách
                        responseBuilder.AppendLine("Shop có chính sách thuê laptop linh hoạt:");
                        responseBuilder.AppendLine("- Sinh viên: Chỉ cần CCCD và thẻ sinh viên, không cần cọc");
                        responseBuilder.AppendLine("- Thanh toán và giao máy tại quầy ngay tức thì");
                        responseBuilder.AppendLine("- Bảo hành đầy đủ trong thời gian thuê");
                    }
                    responseBuilder.AppendLine("\nBạn có muốn tôi tư vấn laptop phù hợp với nhu cầu không?");
                    
                    return new ChatResponse
                    {
                        Message = responseBuilder.ToString(),
                        RecommendedLaptops = null
                    };
                }
                
                // Xử lý yêu cầu về tính năng đặc biệt (mỏng nhẹ, pin trâu, tản nhiệt)
                bool hasFeatureRequest = message.Contains("mỏng nhẹ") || message.Contains("mỏng") || message.Contains("nhẹ") ||
                                        message.Contains("pin trâu") || message.Contains("pin lâu") ||
                                        message.Contains("tản nhiệt") || needs.RequiresThinLight || needs.RequiresLongBattery || needs.RequiresGoodCooling;
                
                if (hasFeatureRequest && laptops.Any())
                {
                    // Tìm laptop phù hợp với yêu cầu tính năng
                    var filteredLaptops = laptops;
                    
                    if (needs.RequiresThinLight || message.Contains("mỏng nhẹ") || message.Contains("mỏng") || message.Contains("nhẹ"))
                    {
                        // Ưu tiên laptop mỏng nhẹ (Ultrabook, XPS, Zenbook, etc.)
                        filteredLaptops = laptops
                            .Where(l => l.Name.ToLower().Contains("xps") || l.Name.ToLower().Contains("ultra") ||
                                       l.Name.ToLower().Contains("zenbook") || l.Name.ToLower().Contains("envy") ||
                                       l.Name.ToLower().Contains("inspiron") || l.Brand.ToLower().Contains("dell") ||
                                       l.Brand.ToLower().Contains("hp") || l.Brand.ToLower().Contains("asus"))
                            .OrderBy(l => l.Price)
                            .Take(3)
                            .ToList();
                        
                        // Nếu không tìm thấy, lấy laptop có CPU i5/i7 (thường là laptop mỏng nhẹ)
                        if (!filteredLaptops.Any())
                        {
                            filteredLaptops = laptops
                                .Where(l => l.Cpu.ToLower().Contains("i5") || l.Cpu.ToLower().Contains("i7") ||
                                           l.Cpu.ToLower().Contains("ryzen 5") || l.Cpu.ToLower().Contains("ryzen 7"))
                                .OrderBy(l => l.Price)
                                .Take(3)
                                .ToList();
                        }
                    }
                    
                    // Nếu vẫn không có, lấy top 3 laptop
                    if (!filteredLaptops.Any())
                    {
                        filteredLaptops = laptops.Take(3).ToList();
                    }
                    
                    responseBuilder.AppendLine("Dựa trên yêu cầu của bạn, tôi xin đề xuất các laptop phù hợp:\n");
                    
                    int index = 1;
                    foreach (var laptop in filteredLaptops)
                    {
                        responseBuilder.AppendLine($"{index}. {laptop.Brand} {laptop.Name}");
                        responseBuilder.AppendLine($"   - Cấu hình: CPU {laptop.Cpu}, RAM {laptop.Ram}, {laptop.Gpu}");
                        responseBuilder.AppendLine($"   - Giá thuê: {laptop.Price:N0} VNĐ/tháng");
                        if (needs.RequiresThinLight)
                            responseBuilder.AppendLine($"   - Thiết kế mỏng nhẹ, phù hợp di chuyển");
                        responseBuilder.AppendLine();
                        index++;
                    }
                    
                    responseBuilder.AppendLine("Bạn có muốn biết thêm chi tiết về laptop nào không? Hoặc bạn có ngân sách cụ thể nào không?");
                    
                    return new ChatResponse
                    {
                        Message = responseBuilder.ToString(),
                        RecommendedLaptops = filteredLaptops
                    };
                }
                
                // Câu hỏi thông thường - hỏi thêm một cách tự nhiên
                responseBuilder.Append("Tôi có thể giúp bạn tìm laptop phù hợp. ");
                
                // Phân tích một phần để hỏi thông minh hơn
                if (message.Contains("marketing") || message.Contains("content") || message.Contains("mỏng nhẹ"))
                {
                    responseBuilder.Append("Với công việc marketing, bạn có cần laptop mỏng nhẹ, pin lâu không? Và ngân sách của bạn là bao nhiêu?");
                }
                else if (message.Contains("tản nhiệt") || message.Contains("gaming"))
                {
                    responseBuilder.Append("Bạn đang tìm laptop gaming với tản nhiệt tốt đúng không? Ngân sách của bạn là bao nhiêu?");
                }
                else
                {
                    responseBuilder.Append("Bạn cần laptop cho mục đích gì? (văn phòng, đồ họa, gaming, hay lập trình?) Và ngân sách của bạn là bao nhiêu?");
                }
                
                return new ChatResponse
                {
                    Message = responseBuilder.ToString(),
                    RecommendedLaptops = null
                };
            }
            
            // Nếu có nhu cầu nhưng không tìm thấy laptop phù hợp
            var noLaptopMessage = new StringBuilder();
            noLaptopMessage.Append("Xin lỗi, hiện tại chúng tôi không có laptop phù hợp với yêu cầu của bạn. ");
            noLaptopMessage.Append("Bạn có thể cho tôi biết thêm về ngân sách và mục đích sử dụng để tôi tìm các lựa chọn khác không?");
            
            return new ChatResponse
            {
                Message = noLaptopMessage.ToString(),
                RecommendedLaptops = null
            };
        }
    }
}

