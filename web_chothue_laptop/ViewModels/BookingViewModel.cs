using System.ComponentModel.DataAnnotations;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.ViewModels
{
    public class BookingViewModel
    {
        public long LaptopId { get; set; }
        
        public Laptop? Laptop { get; set; }

        [Required(ErrorMessage = "Ngày nhận là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày nhận")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Ngày trả là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày trả")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

        [Display(Name = "Số ngày thuê")]
        public int Days { get; set; }

        [Display(Name = "Giá thuê/ngày")]
        public decimal? PricePerDay { get; set; }

        [Display(Name = "Tổng phí thuê")]
        public decimal? TotalPrice { get; set; }

        [Display(Name = "Điều khoản thuê")]
        public bool AgreeToTerms { get; set; }
    }
}



