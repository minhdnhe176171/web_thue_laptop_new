using System.ComponentModel.DataAnnotations;
using web_chothue_laptop.Attributes;

namespace web_chothue_laptop.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn loại tài khoản")]
        [Display(Name = "Loại tài khoản")]
        public string AccountType { get; set; } = string.Empty; // "Customer" hoặc "Student"

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [ValidEmailDomain(ErrorMessage = "Email phải có đuôi @gmail.com, @fpt.edu.vn hoặc các domain hợp lệ khác")]
        [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Số điện thoại phải có đúng 10 số , và không được có kí tự dạng chữ  ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ là bắt buộc")]
        [StringLength(255, ErrorMessage = "Họ không được vượt quá 255 ký tự", MinimumLength = 1)]
        [Display(Name = "Họ")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên là bắt buộc")]
        [StringLength(255, ErrorMessage = "Tên không được vượt quá 255 ký tự", MinimumLength = 1)]
        [Display(Name = "Tên")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã số sinh viên là bắt buộc")]
        [RegularExpression(@"^[A-Za-z]{2}[0-9]{6}$", ErrorMessage = "Mã số sinh viên phải có 2 chữ cái ở đầu và 6 kí tự số đằng sau  (ví dụ: he171199)")]
        [Display(Name = "Mã số sinh viên")]
        public string IdNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        [CustomValidation(typeof(RegisterViewModel), "ValidateDateOfBirth")]
        public DateTime Dob { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có từ {2} đến {1} ký tự", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Custom validation cho ngày sinh
        public static ValidationResult? ValidateDateOfBirth(DateTime dob, ValidationContext context)
        {
            var today = DateTime.Today;
            var minDate = new DateTime(1900, 1, 1);
            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;

            // Kiểm tra ngày sinh từ năm 1900 trở đi
            if (dob < minDate)
            {
                return new ValidationResult("Ngày sinh phải từ năm 1900 trở đi");
            }

            if (dob > today)
            {
                return new ValidationResult("Ngày sinh không được ở tương lai");
            }

            if (age < 16)
            {
                return new ValidationResult("Bạn phải đủ 16 tuổi trở lên để đăng ký");
            }

            if (age > 120)
            {
                return new ValidationResult("Ngày sinh không hợp lệ");
            }

            return ValidationResult.Success;
        }
    }
}

