using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace web_chothue_laptop.ViewModels
{
    public class CreateLaptopViewModel
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "Tên laptop là bắt buộc")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Tên laptop phải từ 5 đến 200 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_\.]+$", ErrorMessage = "Tên laptop chỉ được chứa chữ cái, số, dấu cách, dấu gạch ngang và gạch dưới")]
        [Display(Name = "Tên laptop")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn hãng")]
        [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn hãng hợp lệ")]
        [Display(Name = "Hãng")]
        public long? BrandId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giá")]
        [Display(Name = "Giá")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian đến hạn")]
        [DataType(DataType.Date)]
        [Display(Name = "Thời gian đến hạn")]
        public DateTime? Deadline { get; set; }

        [Display(Name = "Hình ảnh laptop")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageUrl { get; set; }

        [StringLength(100, ErrorMessage = "CPU không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "CPU chỉ được chứa chữ cái, số, dấu cách và dấu gạch ngang")]
        [Display(Name = "CPU")]
        public string? Cpu { get; set; }

        [Display(Name = "RAM")]
        public string? RamSize { get; set; }

        [StringLength(50, ErrorMessage = "Loại RAM không được vượt quá 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Loại RAM chỉ được chứa chữ cái, số, dấu cách và dấu gạch ngang")]
        [Display(Name = "Loại RAM")]
        public string? RamType { get; set; }

        [StringLength(100, ErrorMessage = "Thông tin lưu trữ không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Lưu trữ chỉ được chứa chữ cái, số, dấu cách và dấu gạch ngang")]
        [Display(Name = "Lưu trữ")]
        public string? Storage { get; set; }

        [StringLength(100, ErrorMessage = "GPU không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "GPU chỉ được chứa chữ cái, số, dấu cách và dấu gạch ngang")]
        [Display(Name = "GPU")]
        public string? Gpu { get; set; }

        [StringLength(50, ErrorMessage = "Thông tin màn hình không được vượt quá 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$", ErrorMessage = "Màn hình chỉ được chứa chữ cái, số, dấu cách, dấu gạch ngang và dấu chấm")]
        [Display(Name = "Màn hình")]
        public string? ScreenSize { get; set; }

        [StringLength(100, ErrorMessage = "Hệ điều hành không được vượt quá 100 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-\.]+$", ErrorMessage = "Hệ điều hành chỉ được chứa chữ cái, số, dấu cách, dấu gạch ngang và dấu chấm")]
        [Display(Name = "Hệ điều hành")]
        public string? Os { get; set; }
    }
}