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
        
        // Thông tin laptop detail
        public string? Cpu { get; set; }
        public string? RamSize { get; set; }
        public string? Storage { get; set; }
        public string? Gpu { get; set; }
        public string? ScreenSize { get; set; }
        public string? Os { get; set; }
        
        // Tình tr?ng máy lúc giao
        [Display(Name = "Màn hình")]
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
        [StringLength(500, ErrorMessage = "Ghi chú không ???c v??t quá 500 ký t?")]
        public string? Notes { get; set; }
        
        [Display(Name = "?nh tình tr?ng máy")]
        public IFormFile? ConditionImage { get; set; }
        
        public string? ImageUrl { get; set; }
    }
}
