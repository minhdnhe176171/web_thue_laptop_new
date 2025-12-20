using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Services
{
    public class RagService
    {
        private readonly Swp391LaptopContext _context;

        // Cache đơn giản trong memory để tăng tốc độ
        private static List<LaptopInfo>? _cachedLaptops;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static readonly TimeSpan CacheTTL = TimeSpan.FromSeconds(30); // Cache 30 giây

        public RagService(Swp391LaptopContext context)
        {
            _context = context;
        }

        public class LaptopInfo
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Brand { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string Cpu { get; set; } = string.Empty;
            public string Ram { get; set; } = string.Empty;
            public string Gpu { get; set; } = string.Empty;
            public string Storage { get; set; } = string.Empty;
            public string ScreenSize { get; set; } = string.Empty;
            public string Os { get; set; } = string.Empty;
            public string FullDescription { get; set; } = string.Empty;
        }

        public class CustomerNeeds
        {
            public string? UsageType { get; set; } // "văn phòng", "đồ họa", "gaming", "lập trình", "marketing"
            public decimal? MinBudget { get; set; }
            public decimal? MaxBudget { get; set; }
            public string? CpuRequirement { get; set; }
            public string? RamRequirement { get; set; }
            public string? GpuRequirement { get; set; }

            // Các yêu cầu đặc biệt
            public bool RequiresThinLight { get; set; } // Mỏng nhẹ
            public bool RequiresLongBattery { get; set; } // Pin lâu
            public bool RequiresGoodCooling { get; set; } // Tản nhiệt tốt
        }

        /// <summary>
        /// Lấy danh sách laptop có sẵn, không có người đang thuê
        /// </summary>
        public async Task<List<LaptopInfo>> GetAvailableLaptopsAsync()
        {
            // Kiểm tra cache trước
            if (_cachedLaptops != null && DateTime.Now < _cacheExpiry)
            {
                return _cachedLaptops;
            }

            // Lấy danh sách laptop ID đang có người thuê (active booking)
            var rentedLaptopIds = await _context.Bookings
                .Include(b => b.Status)
                .Where(b => (b.StatusId == 2 || b.StatusId == 10)
                    && b.StartTime <= DateTime.Now
                    && b.EndTime >= DateTime.Now)
                .Select(b => b.LaptopId)
                .Distinct()
                .ToListAsync();

            // Lấy laptop có sẵn và không có người đang thuê
            // Chỉ Select những fields cần thiết để tối ưu
            var laptops = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.LaptopDetails)
                .Where(l => l.Status != null && l.Status.StatusName.ToLower() == "available"
                    && !rentedLaptopIds.Contains(l.Id))
                .ToListAsync();

            var laptopInfos = new List<LaptopInfo>();

            foreach (var laptop in laptops)
            {
                var detail = laptop.LaptopDetails.FirstOrDefault();
                var brandName = laptop.Brand?.BrandName ?? "Không rõ";

                var info = new LaptopInfo
                {
                    Id = laptop.Id,
                    Name = laptop.Name,
                    Brand = brandName,
                    Price = laptop.Price ?? 0,
                    Cpu = detail?.Cpu ?? "Không rõ",
                    Ram = !string.IsNullOrEmpty(detail?.RamSize) && !string.IsNullOrEmpty(detail?.RamType)
                        ? $"{detail.RamSize} {detail.RamType}"
                        : detail?.RamSize ?? "Không rõ",
                    Gpu = detail?.Gpu ?? "Không rõ",
                    Storage = detail?.Storage ?? "Không rõ",
                    ScreenSize = detail?.ScreenSize ?? "Không rõ",
                    Os = detail?.Os ?? "Không rõ"
                };

                // Tạo mô tả đầy đủ cho RAG
                info.FullDescription = $"{info.Brand} {info.Name}. " +
                    $"CPU: {info.Cpu}. " +
                    $"RAM: {info.Ram}. " +
                    $"Card đồ họa: {info.Gpu}. " +
                    $"Ổ cứng: {info.Storage}. " +
                    $"Màn hình: {info.ScreenSize}. " +
                    $"Hệ điều hành: {info.Os}. " +
                    $"Giá thuê: {info.Price:N0} VNĐ/tháng.";

                laptopInfos.Add(info);
            }

            // Lưu vào cache
            _cachedLaptops = laptopInfos;
            _cacheExpiry = DateTime.Now.Add(CacheTTL);

            return laptopInfos;
        }

        /// <summary>
        /// Xóa cache (gọi khi có thay đổi về laptop hoặc booking)
        /// </summary>
        public static void ClearCache()
        {
            _cachedLaptops = null;
            _cacheExpiry = DateTime.MinValue;
        }

        /// <summary>
        /// Tìm laptop phù hợp dựa trên nhu cầu khách hàng
        /// </summary>
        public async Task<List<LaptopInfo>> FindMatchingLaptopsAsync(CustomerNeeds needs)
        {
            var allLaptops = await GetAvailableLaptopsAsync();
            var matches = new List<(LaptopInfo Laptop, int Score)>();

            foreach (var laptop in allLaptops)
            {
                int score = 0;

                // Lọc theo ngân sách
                if (needs.MinBudget.HasValue && laptop.Price < needs.MinBudget.Value)
                    continue;
                if (needs.MaxBudget.HasValue && laptop.Price > needs.MaxBudget.Value)
                    continue;

                // Tính điểm phù hợp dựa trên nhu cầu sử dụng
                if (!string.IsNullOrEmpty(needs.UsageType))
                {
                    var usageLower = needs.UsageType.ToLower();
                    var descriptionLower = laptop.FullDescription.ToLower();

                    if (usageLower.Contains("gaming") || usageLower.Contains("chơi game"))
                    {
                        // Gaming cần GPU mạnh, CPU tốt, RAM lớn
                        if (laptop.Gpu.ToLower().Contains("rtx") || laptop.Gpu.ToLower().Contains("gtx") ||
                            laptop.Gpu.ToLower().Contains("radeon") || laptop.Gpu.ToLower().Contains("discrete"))
                            score += 30;
                        if (laptop.Cpu.ToLower().Contains("i7") || laptop.Cpu.ToLower().Contains("i9") ||
                            laptop.Cpu.ToLower().Contains("ryzen 7") || laptop.Cpu.ToLower().Contains("ryzen 9"))
                            score += 20;
                        if (laptop.Ram.Contains("16") || laptop.Ram.Contains("32"))
                            score += 15;
                    }
                    else if (usageLower.Contains("đồ họa") || usageLower.Contains("graphics") || usageLower.Contains("design"))
                    {
                        // Đồ họa cần GPU mạnh, màn hình lớn, RAM lớn
                        if (laptop.Gpu.ToLower().Contains("rtx") || laptop.Gpu.ToLower().Contains("quadro") ||
                            laptop.Gpu.ToLower().Contains("discrete") || laptop.Gpu.ToLower().Contains("radeon"))
                            score += 30;
                        if (laptop.Ram.Contains("16") || laptop.Ram.Contains("32"))
                            score += 20;
                        if (laptop.ScreenSize.Contains("15") || laptop.ScreenSize.Contains("17"))
                            score += 15;
                        if (laptop.Cpu.ToLower().Contains("i7") || laptop.Cpu.ToLower().Contains("ryzen 7"))
                            score += 10;
                    }
                    else if (usageLower.Contains("lập trình") || usageLower.Contains("programming") || usageLower.Contains("coding"))
                    {
                        // Lập trình cần CPU tốt, RAM đủ, màn hình tốt
                        if (laptop.Cpu.ToLower().Contains("i5") || laptop.Cpu.ToLower().Contains("i7") ||
                            laptop.Cpu.ToLower().Contains("ryzen 5") || laptop.Cpu.ToLower().Contains("ryzen 7"))
                            score += 25;
                        if (laptop.Ram.Contains("8") || laptop.Ram.Contains("16"))
                            score += 20;
                        if (laptop.Storage.Contains("SSD") || laptop.Storage.Contains("ssd"))
                            score += 15;
                    }
                    else if (usageLower.Contains("văn phòng") || usageLower.Contains("office") || usageLower.Contains("học tập"))
                    {
                        // Văn phòng/học tập cần CPU đủ, RAM 8GB+, giá hợp lý
                        if (laptop.Price <= 5000000)
                            score += 30;
                        if (laptop.Ram.Contains("8") || laptop.Ram.Contains("16"))
                            score += 20;
                        if (laptop.Cpu.ToLower().Contains("i3") || laptop.Cpu.ToLower().Contains("i5") ||
                            laptop.Cpu.ToLower().Contains("ryzen 3") || laptop.Cpu.ToLower().Contains("ryzen 5"))
                            score += 15;
                    }
                }

                // Kiểm tra yêu cầu CPU - Xử lý các trường hợp đặc biệt
                if (!string.IsNullOrEmpty(needs.CpuRequirement))
                {
                    var cpuLower = needs.CpuRequirement.ToLower();

                    // Xử lý Macbook - tìm máy tương đương (mỏng nhẹ, premium, hiệu năng cao)
                    if (cpuLower == "macbook")
                    {
                        // Tìm laptop mỏng nhẹ, premium
                        if (laptop.Name.ToLower().Contains("xps") || laptop.Name.ToLower().Contains("ultra") ||
                            laptop.Name.ToLower().Contains("zenbook") || laptop.Name.ToLower().Contains("envy"))
                            score += 40;
                        if (laptop.Cpu.ToLower().Contains("i7") || laptop.Cpu.ToLower().Contains("i9") ||
                            laptop.Cpu.ToLower().Contains("ryzen 7") || laptop.Cpu.ToLower().Contains("ryzen 9"))
                            score += 30;
                        if (laptop.Brand.ToLower().Contains("dell") || laptop.Brand.ToLower().Contains("hp") ||
                            laptop.Brand.ToLower().Contains("asus"))
                            score += 20;
                    }
                    // Xử lý Thinkpad - tìm máy business tương đương
                    else if (cpuLower == "thinkpad")
                    {
                        // Tìm laptop business, bền bỉ
                        if (laptop.Name.ToLower().Contains("latitude") || laptop.Name.ToLower().Contains("elitebook") ||
                            laptop.Name.ToLower().Contains("probook") || laptop.Brand.ToLower().Contains("dell") ||
                            laptop.Brand.ToLower().Contains("hp"))
                            score += 40;
                        if (laptop.Cpu.ToLower().Contains("i5") || laptop.Cpu.ToLower().Contains("i7"))
                            score += 30;
                        // Ưu tiên laptop có RAM 8GB+ cho công việc văn phòng
                        if (laptop.Ram.Contains("8") || laptop.Ram.Contains("16"))
                            score += 20;
                    }
                    // Xử lý Gen 14 - tìm gen 13/12 tương đương
                    else if (cpuLower == "gen 14")
                    {
                        // Tìm CPU gen 13, 12, hoặc Ryzen 7000 series
                        if (laptop.Cpu.ToLower().Contains("gen 13") || laptop.Cpu.ToLower().Contains("gen 12") ||
                            laptop.Cpu.ToLower().Contains("ryzen 7") || laptop.Cpu.ToLower().Contains("ryzen 9"))
                            score += 40;
                        if (laptop.Cpu.ToLower().Contains("i7") || laptop.Cpu.ToLower().Contains("i9"))
                            score += 30;
                    }
                    // Xử lý thông thường
                    else if (laptop.Cpu.ToLower().Contains(cpuLower))
                    {
                        score += 20;
                    }
                }

                // Kiểm tra yêu cầu RAM
                if (!string.IsNullOrEmpty(needs.RamRequirement))
                {
                    if (laptop.Ram.Contains(needs.RamRequirement))
                        score += 20;
                }

                // Kiểm tra yêu cầu GPU
                if (!string.IsNullOrEmpty(needs.GpuRequirement))
                {
                    var gpuLower = needs.GpuRequirement.ToLower();
                    if (laptop.Gpu.ToLower().Contains(gpuLower))
                        score += 20;
                }

                // Xử lý yêu cầu về tính năng đặc biệt
                if (needs.RequiresThinLight)
                {
                    // Ưu tiên laptop mỏng nhẹ
                    if (laptop.Name.ToLower().Contains("ultra") || laptop.Name.ToLower().Contains("xps") ||
                        laptop.Name.ToLower().Contains("zenbook") || laptop.Name.ToLower().Contains("envy") ||
                        laptop.Name.ToLower().Contains("inspiron") || laptop.Brand.ToLower().Contains("dell") ||
                        laptop.Brand.ToLower().Contains("hp") || laptop.Brand.ToLower().Contains("asus"))
                        score += 35;
                    // CPU i5/i7 thường là laptop mỏng nhẹ
                    if (laptop.Cpu.ToLower().Contains("i5") || laptop.Cpu.ToLower().Contains("i7") ||
                        laptop.Cpu.ToLower().Contains("ryzen 5") || laptop.Cpu.ToLower().Contains("ryzen 7"))
                        score += 20;
                }

                if (needs.RequiresLongBattery)
                {
                    // Laptop mỏng nhẹ thường có pin tốt hơn
                    if (laptop.Name.ToLower().Contains("ultra") || laptop.Name.ToLower().Contains("xps") ||
                        laptop.Name.ToLower().Contains("zenbook") || laptop.Name.ToLower().Contains("envy"))
                        score += 30;
                    if (laptop.Cpu.ToLower().Contains("i5") || laptop.Cpu.ToLower().Contains("i7"))
                        score += 20; // CPU tiết kiệm pin
                }

                if (needs.RequiresGoodCooling)
                {
                    // Ưu tiên laptop gaming có tản nhiệt tốt
                    if (laptop.Name.ToLower().Contains("tuf") || laptop.Name.ToLower().Contains("victus") ||
                        laptop.Name.ToLower().Contains("legion") || laptop.Name.ToLower().Contains("predator") ||
                        laptop.Name.ToLower().Contains("gaming"))
                        score += 35;
                    if (laptop.Gpu.ToLower().Contains("rtx") || laptop.Gpu.ToLower().Contains("gtx"))
                        score += 20;
                }

                // Nếu có bất kỳ yêu cầu nào hoặc không có filter nào, thêm vào danh sách
                if (score > 0 || (!needs.MinBudget.HasValue && !needs.MaxBudget.HasValue &&
                    string.IsNullOrEmpty(needs.UsageType) && !needs.RequiresThinLight &&
                    !needs.RequiresLongBattery && !needs.RequiresGoodCooling))
                {
                    matches.Add((laptop, score));
                }
            }

            // Sắp xếp theo điểm số giảm dần, sau đó theo giá tăng dần
            return matches
                .OrderByDescending(m => m.Score)
                .ThenBy(m => m.Laptop.Price)
                .Select(m => m.Laptop)
                .ToList();
        }

        /// <summary>
        /// Phân tích nhu cầu từ tin nhắn của khách hàng
        /// </summary>
        public CustomerNeeds AnalyzeCustomerNeeds(string message, List<string> conversationHistory)
        {
            var needs = new CustomerNeeds();
            var messageLower = message.ToLower();

            // Kết hợp với lịch sử hội thoại để hiểu context tốt hơn
            var fullContext = string.Join(" ", conversationHistory) + " " + messageLower;

            // Phát hiện loại sử dụng - Mở rộng để nhận diện nhiều ngành nghề hơn
            if (fullContext.Contains("gaming") || fullContext.Contains("chơi game") || fullContext.Contains("game") ||
                fullContext.Contains("streaming") || fullContext.Contains("cày game") || fullContext.Contains("gameplay"))
                needs.UsageType = "gaming";
            else if (fullContext.Contains("đồ họa") || fullContext.Contains("design") || fullContext.Contains("photoshop") ||
                     fullContext.Contains("illustrator") || fullContext.Contains("premiere") || fullContext.Contains("after effects") ||
                     fullContext.Contains("autocad") || fullContext.Contains("3d") || fullContext.Contains("render") ||
                     fullContext.Contains("video editing") || fullContext.Contains("chỉnh sửa video") ||
                     fullContext.Contains("màn hình rời") || fullContext.Contains("màn hình để làm đồ họa"))
                needs.UsageType = "đồ họa";
            else if (fullContext.Contains("lập trình") || fullContext.Contains("coding") || fullContext.Contains("programming") ||
                     fullContext.Contains("dev") || fullContext.Contains("developer") || fullContext.Contains("code") ||
                     fullContext.Contains("phát triển phần mềm") || fullContext.Contains("software development"))
                needs.UsageType = "lập trình";
            else if (fullContext.Contains("marketing") || fullContext.Contains("content creator") ||
                     fullContext.Contains("di chuyển nhiều") || fullContext.Contains("làm việc ngoài"))
                needs.UsageType = "marketing"; // Đặc biệt: mỏng nhẹ + pin tốt

            // Phát hiện yêu cầu đặc biệt về tính năng
            if (fullContext.Contains("mỏng nhẹ") || fullContext.Contains("mỏng") || fullContext.Contains("nhẹ") ||
                fullContext.Contains("ultrabook") || fullContext.Contains("thin") || fullContext.Contains("light"))
                needs.RequiresThinLight = true;

            if (fullContext.Contains("pin trâu") || fullContext.Contains("pin lâu") || fullContext.Contains("pin tốt") ||
                fullContext.Contains("battery") || fullContext.Contains("pin dùng lâu"))
                needs.RequiresLongBattery = true;

            if (fullContext.Contains("tản nhiệt") || fullContext.Contains("tản nhiệt tốt") ||
                fullContext.Contains("cooling") || fullContext.Contains("nhiệt độ"))
                needs.RequiresGoodCooling = true;
            else if (fullContext.Contains("văn phòng") || fullContext.Contains("office") || fullContext.Contains("word") ||
                     fullContext.Contains("excel") || fullContext.Contains("học tập") || fullContext.Contains("học") ||
                     fullContext.Contains("powerpoint") || fullContext.Contains("presentation") || fullContext.Contains("soạn thảo"))
                needs.UsageType = "văn phòng";

            // Phát hiện câu hỏi về giá trực tiếp (ngay cả khi không có usage type)
            // (Không cần lưu vào needs, chỉ để tham khảo)

            // Phát hiện ngân sách - Mở rộng pattern để nhận diện nhiều cách nói hơn
            var budgetPatterns = new[]
            {
                // Pattern: "200 nghìn", "200 ngàn"
                new { Pattern = @"(\d+)\s*(?:nghìn|ngàn|nghin|ngan)(?:\s*(?:đồng|vnđ|vnd))?\b", Multiplier = 1000m },
                // Pattern: "200k" (k = nghìn)
                new { Pattern = @"(\d+)\s*k\b(?!\s*(?:triệu|trieu|tr|triệu đồng|trieu dong))", Multiplier = 1000m },
                // Pattern: "5 triệu", "5 trieu"
                new { Pattern = @"(\d+)\s*(?:triệu|trieu|tr)(?:\s*(?:đồng|vnđ|vnd))?\b", Multiplier = 1000000m },
                // Pattern: "2000k", "2000k đồng" (nghìn)
                new { Pattern = @"(\d+)\s*k\s*(?:đồng|vnđ|vnd)?\b", Multiplier = 1000m },
                // Pattern số lớn: nếu có "triệu" hoặc "tr" đứng trước hoặc sau số
                new { Pattern = @"(?:triệu|trieu|tr)\s*(\d+)", Multiplier = 1000000m }
            };

            foreach (var pattern in budgetPatterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(fullContext, pattern.Pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (decimal.TryParse(match.Groups[1].Value, out var amount))
                    {
                        var budget = amount * pattern.Multiplier;

                        // Xác định min/max dựa trên từ khóa
                        var contextLower = fullContext.ToLower();
                        // messageLower đã được khai báo ở đầu hàm, không khai báo lại

                        // Tìm context xung quanh số tiền (50 ký tự trước và sau)
                        var startIdx = Math.Max(0, match.Index - 50);
                        var endIdx = Math.Min(fullContext.Length, match.Index + match.Length + 50);
                        var nearbyContext = fullContext.Substring(startIdx, endIdx - startIdx).ToLower();

                        // Kiểm tra từ khóa "dưới" (ưu tiên cao nhất)
                        if (nearbyContext.Contains("dưới") || nearbyContext.Contains("tối đa") ||
                            nearbyContext.Contains("max") || nearbyContext.Contains("nhỏ hơn") ||
                            nearbyContext.Contains("ít hơn") || nearbyContext.Contains("khoảng dưới") ||
                            nearbyContext.Contains("không quá") || nearbyContext.Contains("dưới mức") ||
                            messageLower.Contains("dưới") && (messageLower.IndexOf("dưới") < messageLower.IndexOf(match.Value.ToLower()) + 20))
                        {
                            // Nếu đã có MaxBudget, lấy giá trị nhỏ hơn
                            if (!needs.MaxBudget.HasValue || budget < needs.MaxBudget.Value)
                                needs.MaxBudget = budget;
                        }
                        // Kiểm tra từ khóa "trên"
                        else if (nearbyContext.Contains("trên") || nearbyContext.Contains("tối thiểu") ||
                                 nearbyContext.Contains("min") || nearbyContext.Contains("lớn hơn") ||
                                 nearbyContext.Contains("nhiều hơn") || nearbyContext.Contains("khoảng trên") ||
                                 nearbyContext.Contains("ít nhất") || nearbyContext.Contains("từ mức") ||
                                 messageLower.Contains("trên") && (messageLower.IndexOf("trên") < messageLower.IndexOf(match.Value.ToLower()) + 20))
                        {
                            // Nếu đã có MinBudget, lấy giá trị lớn hơn
                            if (!needs.MinBudget.HasValue || budget > needs.MinBudget.Value)
                                needs.MinBudget = budget;
                        }
                        // Kiểm tra khoảng giá
                        else if (nearbyContext.Contains("đến") || nearbyContext.Contains("-") ||
                                 nearbyContext.Contains("khoảng") || nearbyContext.Contains("tầm") ||
                                 nearbyContext.Contains("khoản") || nearbyContext.Contains("giữa") ||
                                 messageLower.Contains("từ") || messageLower.Contains("khoảng"))
                        {
                            // Khoảng giá: thử tìm giá thứ 2 hoặc dùng ±20%
                            // Nếu đã có giá khác, tạo khoảng
                            if (needs.MinBudget.HasValue && budget > needs.MinBudget.Value)
                            {
                                needs.MaxBudget = budget;
                            }
                            else if (needs.MaxBudget.HasValue && budget < needs.MaxBudget.Value)
                            {
                                needs.MinBudget = budget;
                            }
                            else
                            {
                                // Mặc định là khoảng ±20%
                                needs.MinBudget = budget * 0.8m;
                                needs.MaxBudget = budget * 1.2m;
                            }
                        }
                        else
                        {
                            // Kiểm tra toàn bộ câu hỏi để xem có từ "dưới" không
                            // Đặc biệt xử lý trường hợp "laptop nào giá dưới 200 nghìn"
                            if (messageLower.Contains("dưới") || contextLower.Contains("giá dưới"))
                            {
                                if (!needs.MaxBudget.HasValue || budget < needs.MaxBudget.Value)
                                    needs.MaxBudget = budget;
                            }
                            // Kiểm tra "trên"
                            else if (messageLower.Contains("trên") || contextLower.Contains("giá trên"))
                            {
                                if (!needs.MinBudget.HasValue || budget > needs.MinBudget.Value)
                                    needs.MinBudget = budget;
                            }
                            else
                            {
                                // Mặc định là khoảng ±20% cho câu hỏi giá chung chung
                                if (!needs.MinBudget.HasValue && !needs.MaxBudget.HasValue)
                                {
                                    needs.MinBudget = budget * 0.8m;
                                    needs.MaxBudget = budget * 1.2m;
                                }
                            }
                        }
                    }
                }
            }

            // Phát hiện yêu cầu CPU
            if (fullContext.Contains("i3") || fullContext.Contains("i5") || fullContext.Contains("i7") || fullContext.Contains("i9") ||
                fullContext.Contains("ryzen") || fullContext.Contains("intel") || fullContext.Contains("amd"))
            {
                if (fullContext.Contains("i3"))
                    needs.CpuRequirement = "i3";
                else if (fullContext.Contains("i5"))
                    needs.CpuRequirement = "i5";
                else if (fullContext.Contains("i7"))
                    needs.CpuRequirement = "i7";
                else if (fullContext.Contains("i9"))
                    needs.CpuRequirement = "i9";
                else if (fullContext.Contains("ryzen"))
                    needs.CpuRequirement = "ryzen";
            }

            // Phát hiện yêu cầu RAM
            if (fullContext.Contains("8gb") || fullContext.Contains("16gb") || fullContext.Contains("32gb") ||
                fullContext.Contains("8 gb") || fullContext.Contains("16 gb") || fullContext.Contains("32 gb"))
            {
                if (fullContext.Contains("8"))
                    needs.RamRequirement = "8";
                else if (fullContext.Contains("16"))
                    needs.RamRequirement = "16";
                else if (fullContext.Contains("32"))
                    needs.RamRequirement = "32";
            }

            return needs;
        }
    }
}