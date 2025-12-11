using System.ComponentModel.DataAnnotations;

namespace web_chothue_laptop.ViewModels
{
    public class CreateLaptopViewModel
    {
        public long? Id { get; set; }

        [Required(ErrorMessage = "Tên laptop là bắt buộc")]
        [Display(Name = "Tên laptop")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn hãng")]
        [Display(Name = "Hãng")]
        public long? BrandId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giá")]
        [Display(Name = "Giá")]
        public decimal? Price { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian đến hạn")]
        [DataType(DataType.Date)]
        [Display(Name = "Thời gian đến hạn")]
        public DateTime? Deadline { get; set; }

        [Display(Name = "CPU")]
        public string? Cpu { get; set; }

        [Display(Name = "RAM")]
        public string? RamSize { get; set; }

        [Display(Name = "Lưu trữ")]
        public string? Storage { get; set; }

        [Display(Name = "GPU")]
        public string? Gpu { get; set; }
    }
}
