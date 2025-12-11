using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace web_chothue_laptop.ViewModels
{
    public class ProfileViewModel
    {
        public long UserId { get; set; }
        
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
        
        [Display(Name = "Họ")]
        [Required(ErrorMessage = "Họ là bắt buộc")]
        public string FirstName { get; set; } = string.Empty;
        
        [Display(Name = "Tên")]
        [Required(ErrorMessage = "Tên là bắt buộc")]
        public string LastName { get; set; } = string.Empty;
        
        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải có đúng 10 chữ số")]
        [Remote("CheckPhoneExists", "Profile", AdditionalFields = "UserId", ErrorMessage = "Số điện thoại này đã được sử dụng bởi tài khoản khác")]
        public string? Phone { get; set; }
        
        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(ProfileViewModel), "ValidateAge")]
        public DateTime? Dob { get; set; }

        public static ValidationResult? ValidateAge(DateTime? dob, ValidationContext context)
        {
            if (dob.HasValue)
            {
                var today = DateTime.Today;
                var age = today.Year - dob.Value.Year;
                
                // Kiểm tra nếu chưa đến sinh nhật trong năm nay
                if (dob.Value.Date > today.AddYears(-age))
                {
                    age--;
                }
                
                if (age < 16)
                {
                    return new ValidationResult("Bạn phải trên 16 tuổi để sử dụng dịch vụ này");
                }
            }
            return ValidationResult.Success;
        }
        
        [Display(Name = "Role ID")]
        public long? RoleId { get; set; }
        
        [Display(Name = "Tên Role")]
        public string? RoleName { get; set; }
        
        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }
    }
}

