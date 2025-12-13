using System.ComponentModel.DataAnnotations;

namespace web_chothue_laptop.ViewModels
{
    public class TicketViewModel
    {
        public long LaptopId { get; set; }
        
        public long? BookingId { get; set; }

        [Required(ErrorMessage = "Mô tả lỗi là bắt buộc")]
        [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự", MinimumLength = 10)]
        [Display(Name = "Mô tả lỗi")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Hình ảnh lỗi")]
        public IFormFile? ErrorImage { get; set; }

        public string? ErrorImageUrl { get; set; }
    }
}



