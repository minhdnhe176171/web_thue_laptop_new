using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace web_chothue_laptop.Attributes
{
    public class ValidEmailDomainAttribute : ValidationAttribute
    {
        private static readonly string[] AllowedDomains = {
            "@gmail.com",
            "@fpt.edu.vn",
            "@outlook.com",
            "@yahoo.com",
            "@hotmail.com",
            "@fe.edu.vn"
        };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success; // Let Required attribute handle this
            }

            var email = value.ToString()!.Trim().ToLower();

            // Kiểm tra format email cơ bản
            if (!Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                return new ValidationResult("Email không đúng định dạng");
            }

            // Kiểm tra domain
            bool isValidDomain = AllowedDomains.Any(domain => email.EndsWith(domain));

            if (!isValidDomain)
            {
                var allowedDomainsList = string.Join(", ", AllowedDomains);
                return new ValidationResult($"Email phải có đuôi một trong các domain sau: {allowedDomainsList}");
            }

            return ValidationResult.Success;
        }
    }
}

