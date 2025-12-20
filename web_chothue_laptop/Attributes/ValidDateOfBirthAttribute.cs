using System.ComponentModel.DataAnnotations;

namespace web_chothue_laptop.Attributes
{
    public class ValidDateOfBirthAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success; // Let Required attribute handle this
            }

            if (!(value is DateTime dob))
            {
                return new ValidationResult("Ngày sinh không hợp lệ");
            }

            var today = DateTime.Today; // So với thời gian thực
            var minDate = new DateTime(1900, 1, 1);

            // Kiểm tra ngày sinh từ năm 1900 trở đi
            if (dob < minDate)
            {
                return new ValidationResult("Ngày sinh phải từ năm 1900 trở đi");
            }

            // Kiểm tra ngày sinh không được ở tương lai
            if (dob > today)
            {
                return new ValidationResult("Ngày sinh không được ở tương lai");
            }

            // Tính tuổi CHÍNH XÁC so với thời gian thực (tính từ ngày, tháng, năm)
            var age = today.Year - dob.Year;
            // Trừ 1 tuổi nếu chưa đến ngày sinh trong năm hiện tại
            // Xử lý trường hợp ngày sinh 29/02 (năm nhuận)
            DateTime birthdayThisYear;
            try
            {
                birthdayThisYear = new DateTime(today.Year, dob.Month, dob.Day);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Nếu năm hiện tại không phải năm nhuận và ngày sinh là 29/02, dùng 28/02
                birthdayThisYear = new DateTime(today.Year, dob.Month, 28);
            }
            
            // Nếu chưa đến ngày sinh trong năm hiện tại thì trừ 1 tuổi
            if (birthdayThisYear > today)
            {
                age--;
            }

            // KIỂM TRA PHẢI ĐỦ 18 TUỔI (>= 18) - BẮT BUỘC - KHÔNG ĐỦ 18 TUỔI THÌ BÁO LỖI NGAY
            if (age < 18)
            {
                return new ValidationResult("Bạn phải đủ 18 tuổi trở lên để đăng ký. Vui lòng chọn lại ngày sinh");
            }

            if (age > 120)
            {
                return new ValidationResult("Ngày sinh không hợp lệ");
            }

            return ValidationResult.Success;
        }
    }
}
