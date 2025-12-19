using System.ComponentModel.DataAnnotations;

namespace web_chothue_laptop.ViewModels
{
    public class DeliveryViewModel
    {
        public long BookingId { get; set; }
        
        // Thông tin booking
        public string? CustomerName { get; set; }
        public string? LaptopName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? LaptopImageUrl { get; set; }
        // [MỚI] Thêm 3 trường này
        public string? IdCardUrl { get; set; }     // Link ảnh CCCD
        public string? StudentCardUrl { get; set; } // Link thẻ sinh viên

        // Checkbox bắt buộc
        public bool IsIdentityVerified { get; set; }

        // Thông tin laptop detail
        public string? Cpu { get; set; }
        public string? RamSize { get; set; }
        public string? Storage { get; set; }
        public string? Gpu { get; set; }
        public string? ScreenSize { get; set; }
        public string? Os { get; set; }
        
        // T́nh tr?ng máy lúc giao
        [Display(Name = "Màn h́nh")]
        public bool ScreenCondition { get; set; } = true;
        
        [Display(Name = "Bàn phím")]
        public bool KeyboardCondition { get; set; } = true;
        
        [Display(Name = "Chu?t")]
        public bool MouseCondition { get; set; } = true;
        
        [Display(Name = "S?c")]
        public bool ChargerCondition { get; set; } = true;
        
        [Display(Name = "V? máy")]
        public bool BodyCondition { get; set; } = true;
        
        [Display(Name = "Pin (%)")]
        [Range(0, 100, ErrorMessage = "Pin ph?i t? 0-100%")]
        public int BatteryLevel { get; set; } = 100;
        
        [Display(Name = "Ghi chú")]
        [StringLength(500, ErrorMessage = "Ghi chú không ???c v??t quá 500 kư t?")]
        public string? Notes { get; set; }
        
        [Display(Name = "?nh t́nh tr?ng máy")]
        public IFormFile? ConditionImage { get; set; }
        
        public string? ImageUrl { get; set; }
    }
}
