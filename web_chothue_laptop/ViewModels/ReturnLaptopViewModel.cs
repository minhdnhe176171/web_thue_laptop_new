using System;
using System.ComponentModel.DataAnnotations;

namespace web_chothue_laptop.ViewModels
{
    public class ReturnLaptopViewModel
    {
        // ==========================================
        // PHẦN 1: THÔNG TIN HIỂN THỊ (READONLY)
        // Thêm dấu ? vào sau string để cho phép null
        // ==========================================

        public long LaptopId { get; set; }

        [Display(Name = "Tên Laptop")]
        public string? LaptopName { get; set; } // <--- Thêm dấu ?

        // ID của Student để backend dùng gửi Notification/Email
        public long? StudentId { get; set; }

        [Display(Name = "Chủ sở hữu")]
        public string? StudentName { get; set; } // <--- Thêm dấu ?

        [Display(Name = "Số điện thoại")]
        public string? StudentPhone { get; set; } // <--- Thêm dấu ?

        [Display(Name = "Email liên hệ")]
        public string? StudentEmail { get; set; } // <--- Thêm dấu ?

        [Display(Name = "Hết hạn ký gửi")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? ContractEndDate { get; set; }

        // ==========================================
        // PHẦN 2: FORM NHẬP LIỆU (INPUT)
        // Staff sẽ nhập phần này
        // ==========================================

        [Display(Name = "Địa điểm nhận máy")]
        public string PickupLocation { get; set; } = "Toà Alpha, L300"; // Đã có giá trị mặc định nên không lỗi

        [Required(ErrorMessage = "Vui lòng chọn thời gian hẹn.")]
        [Display(Name = "Thời gian hẹn nhận máy")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentTime { get; set; } = DateTime.Now.AddHours(2);
    }
}