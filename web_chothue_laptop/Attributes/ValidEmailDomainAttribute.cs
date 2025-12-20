using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace web_chothue_laptop.Attributes
{
    public class ValidEmailDomainAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success; // Let Required attribute handle this
            }

            var email = value.ToString()!.Trim();

            // Kiểm tra có ký tự @ không
            if (!email.Contains("@"))
            {
                return new ValidationResult("Email phải có ký tự @. Vui lòng kiểm tra lại định dạng email");
            }

            // Tách email thành phần local (trước @) và domain (sau @)
            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return new ValidationResult("Email không đúng định dạng");
            }

            var localPart = parts[0];
            var domainPart = parts[1].ToLower().Trim();

            // Kiểm tra phần trước @ không được rỗng
            if (string.IsNullOrWhiteSpace(localPart))
            {
                return new ValidationResult("Email phải có phần trước @");
            }

            // Kiểm tra phần trước @ không được có các ký tự đặc biệt: ! @ # $ % ^ &* ()+ [ ] \ : ; " ' < > ? / | ,
            // Chỉ cho phép chữ cái (a-z, A-Z), số (0-9), dấu chấm (.), dấu gạch dưới (_), dấu gạch ngang (-)
            // Regex pattern để bắt tất cả các ký tự đặc biệt không được phép
            if (Regex.IsMatch(localPart, @"[!@#\$%\^&\*\(\)\+\[\]\\:;""'<>?/|,]"))
            {
                return new ValidationResult("Phần trước @ của email không được chứa các ký tự đặc biệt: ! @ # $ % ^ &* ()+ [ ] \\ : ; \" ' < > ? / | ,. Vui lòng nhập lại");
            }
            
            // Đảm bảo chỉ chứa các ký tự hợp lệ: chữ, số, dấu chấm, gạch dưới, gạch ngang
            if (!Regex.IsMatch(localPart, @"^[a-zA-Z0-9._-]+$"))
            {
                return new ValidationResult("Phần trước @ của email chỉ được chứa chữ cái, số, dấu chấm (.), dấu gạch dưới (_) và dấu gạch ngang (-). Vui lòng nhập lại");
            }

            // Kiểm tra domain không được rỗng
            if (string.IsNullOrWhiteSpace(domainPart))
            {
                return new ValidationResult("Email phải có tên miền sau @");
            }

            // Kiểm tra domain phải có định dạng tên miền chuẩn
            // Chấp nhận các tên miền như: gmail.com, hotmail.com, fpt.edu.vn, example.com, example.vn, example.edu.vn, v.v.
            // Format: domain.tld hoặc domain.subdomain.tld
            // TLD có thể là: .com, .vn, .edu.vn, .com.vn, .org, .net, .edu, .org.vn, .net.vn, .gov.vn, v.v.
            
            // Regex để kiểm tra định dạng tên miền chuẩn
            // Cho phép: chữ, số, dấu chấm, dấu gạch ngang
            // Phải có ít nhất một dấu chấm (để phân cách tên miền và TLD)
            // Phần TLD phải có ít nhất 2 ký tự (ví dụ: .com, .vn, .edu)
            // Pattern: một hoặc nhiều phần domain (chữ, số, gạch ngang) được phân cách bởi dấu chấm, kết thúc bằng TLD ít nhất 2 ký tự
            if (!Regex.IsMatch(domainPart, @"^([a-z0-9]+(-[a-z0-9]+)*\.)+[a-z]{2,}$"))
            {
                return new ValidationResult("Tên miền email không đúng định dạng. Vui lòng nhập tên miền chuẩn (ví dụ: gmail.com, hotmail.com, fpt.edu.vn, example.com, example.vn)");
            }

            return ValidationResult.Success;
        }
    }
}







