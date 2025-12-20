using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace web_chothue_laptop.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOTPEmailAsync(string toEmail, string otpCode)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("Email settings not configured");
                    return false;
                }

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, "Dịch Vụ Thuê Laptop"),
                    Subject = "Mã OTP xác thực tài khoản",
                    Body = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2 style='color: #0d6efd;'>Mã OTP xác thực</h2>
                            <p>Xin chào,</p>
                            <p>Mã OTP của bạn là: <strong style='font-size: 24px; color: #0d6efd;'>{otpCode}</strong></p>
                            <p>Mã này có hiệu lực trong 10 phút.</p>
                            <p>Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                            <hr>
                            <p style='color: #666; font-size: 12px;'>Đây là email tự động, vui lòng không trả lời.</p>
                        </body>
                        </html>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"OTP email sent successfully to {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending OTP email to {toEmail}");
                return false;
            }
        }
    }
}



