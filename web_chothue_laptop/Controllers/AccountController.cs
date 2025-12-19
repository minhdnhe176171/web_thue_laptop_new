using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using web_chothue_laptop.Models;
using web_chothue_laptop.Services;
using web_chothue_laptop.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace web_chothue_laptop.Controllers
{
    public class AccountController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(Swp391LaptopContext context, EmailService emailService, ILogger<AccountController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            return View();
        }


        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (model != null) model.Email = model.Email?.Trim() ?? string.Empty;
            if (!ModelState.IsValid) return View(model);

            // Tìm User
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Status)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            // Validate User & Password
            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View(model);
            }

            // Validate Active
            if (user.Status?.StatusName?.Trim().ToLower() != "active")
            {
                ModelState.AddModelError("", "Tài khoản chưa được kích hoạt hoặc đã bị khóa");
                return View(model);
            }

            // --- XỬ LÝ ROLE ---
            string roleName = user.Role?.RoleName?.Trim() ?? "Customer"; // Mặc định là Customer nếu null

            // --- TẠO CLAIMS (Giấy thông hành) ---
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserId", user.Id.ToString()),
                // Dòng này cực quan trọng để [Authorize(Roles="...")] hoạt động
                new Claim(ClaimTypes.Role, roleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Nhớ đăng nhập
                ExpiresUtc = DateTime.UtcNow.AddMinutes(60)
            };

            // --- GHI COOKIE VÀO TRÌNH DUYỆT ---
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Lưu Session (Phụ trợ)
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserRole", roleName);

            // --- ĐIỀU HƯỚNG DỰA TRÊN ROLE ---
            // Dùng ToLower() để so sánh cho chắc chắn
            switch (roleName.ToLower())
            {
                case "staff":
                    return RedirectToAction("Index", "Staff"); // Vào thẳng Staff

                case "technical":
                    return RedirectToAction("Index", "Technical"); // Vào thẳng Technical

                case "admin":
                case "manager":
                    return RedirectToAction("LaptopManagement", "Manager");

                default:
                    return RedirectToAction("Index", "Home"); // Customer/Student về Home
            }
        }
        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Trim các trường string
            if (model != null)
            {
                model.Email = model.Email?.Trim() ?? string.Empty;
                model.Phone = model.Phone?.Trim() ?? string.Empty;
                model.FirstName = model.FirstName?.Trim() ?? string.Empty;
                model.LastName = model.LastName?.Trim() ?? string.Empty;
                model.IdNo = model.IdNo?.Trim() ?? string.Empty; // Giữ nguyên, không tự động chuyển đổi
                model.AccountType = model.AccountType?.Trim() ?? string.Empty;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra AccountType hợp lệ
            if (model.AccountType != "Customer" && model.AccountType != "Student")
            {
                ModelState.AddModelError("AccountType", "Vui lòng chọn loại tài khoản hợp lệ");
                return View(model);
            }

            // Kiểm tra các trường không chỉ có khoảng trắng
            if (string.IsNullOrWhiteSpace(model.FirstName))
            {
                ModelState.AddModelError("FirstName", "Họ không được để trống");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.LastName))
            {
                ModelState.AddModelError("LastName", "Tên không được để trống");
                return View(model);
            }

            // Kiểm tra mã số sinh viên format: 2 chữ cái + 6 số (không phân biệt hoa thường)
            if (string.IsNullOrWhiteSpace(model.IdNo))
            {
                ModelState.AddModelError("IdNo", "Mã số sinh viên không được để trống");
                return View(model);
            }

            // Validate format: 2 chữ cái (không phân biệt hoa thường) + 6 số
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.IdNo, @"^[A-Za-z]{2}[0-9]{6}$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                ModelState.AddModelError("IdNo", "Mã số sinh viên phải có 2 chữ cái đầu tiên và 6 chữ số sau (ví dụ: HE171199)");
                return View(model);
            }

            // Kiểm tra số điện thoại phải đúng 10 số
            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError("Phone", "Số điện thoại không được để trống");
                return View(model);
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone, @"^[0-9]{10}$"))
            {
                ModelState.AddModelError("Phone", "Số điện thoại phải có đúng 10 chữ số");
                return View(model);
            }

            // Kiểm tra ngày sinh từ năm 1900 trở đi
            var minDate = new DateTime(1900, 1, 1);
            if (model.Dob < minDate)
            {
                ModelState.AddModelError("Dob", "Ngày sinh phải từ năm 1900 trở đi");
                return View(model);
            }

            if (model.Dob > DateTime.Today)
            {
                ModelState.AddModelError("Dob", "Ngày sinh không được ở tương lai");
                return View(model);
            }

            // Kiểm tra email đã tồn tại
            var emailLower = model.Email.ToLower();
            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == emailLower))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng");
                return View(model);
            }

            // Kiểm tra email trong Customer hoặc Student
            if (model.AccountType == "Customer" && await _context.Customers.AnyAsync(c => c.Email.ToLower() == emailLower))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng");
                return View(model);
            }

            if (model.AccountType == "Student" && await _context.Students.AnyAsync(s => s.Email.ToLower() == emailLower))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng");
                return View(model);
            }

            // Tạo User tạm thời (chưa kích hoạt)
            var user = new User
            {
                Email = model.Email,
                PasswordHash = HashPassword(model.Password),
                CreatedDate = DateTime.Now,
                StatusId = await GetStatusIdAsync("pending") // Trạng thái chờ xác thực
            };

            // Lấy RoleId dựa trên AccountType
            if (model.AccountType == "Customer")
            {
                user.RoleId = await GetRoleIdAsync("customer");
            }
            else if (model.AccountType == "Student")
            {
                user.RoleId = await GetRoleIdAsync("student");
            }
            else
            {
                ModelState.AddModelError("AccountType", "Loại tài khoản không hợp lệ");
                return View(model);
            }

            // Tạo và gửi OTP
            var otp = GenerateOTP();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.Now.AddMinutes(10);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Lưu thông tin đăng ký vào session để xác thực OTP
            HttpContext.Session.SetString("RegisterUserId", user.Id.ToString());
            HttpContext.Session.SetString("RegisterEmail", model.Email);
            HttpContext.Session.SetString("RegisterAccountType", model.AccountType);
            HttpContext.Session.SetString("RegisterFirstName", model.FirstName);
            HttpContext.Session.SetString("RegisterLastName", model.LastName);
            HttpContext.Session.SetString("RegisterPhone", model.Phone);
            HttpContext.Session.SetString("RegisterIdNo", model.IdNo);
            HttpContext.Session.SetString("RegisterDob", model.Dob.ToString("yyyy-MM-dd"));

            // Gửi email OTP
            var emailSent = await _emailService.SendOTPEmailAsync(model.Email, otp);
            if (!emailSent)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                ModelState.AddModelError("", "Không thể gửi email OTP. Vui lòng thử lại sau.");
                return View(model);
            }

            return RedirectToAction("VerifyOTP", new { email = model.Email, type = "register" });
        }

        // GET: Account/VerifyOTP
        public IActionResult VerifyOTP(string email, string type)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(type))
            {
                return RedirectToAction("Register");
            }

            var model = new VerifyOTPViewModel
            {
                Email = email
            };

            ViewBag.Type = type; // "register" hoặc "forgotpassword"
            return View(model);
        }

        // POST: Account/VerifyOTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(VerifyOTPViewModel model, string type)
        {
            // Trim các trường
            if (model != null)
            {
                model.Email = model.Email?.Trim() ?? string.Empty;
                model.OTP = model.OTP?.Trim() ?? string.Empty;
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Type = type;
                return View(model);
            }

            // Kiểm tra email và OTP không rỗng
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
                ViewBag.Type = type;
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.OTP))
            {
                ModelState.AddModelError("OTP", "Mã OTP là bắt buộc");
                ViewBag.Type = type;
                return View(model);
            }

            // Kiểm tra OTP chỉ chứa số
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.OTP, @"^[0-9]{6}$"))
            {
                ModelState.AddModelError("OTP", "Mã OTP phải là 6 chữ số");
                ViewBag.Type = type;
                return View(model);
            }

            var emailLower = model.Email.ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản");
                ViewBag.Type = type;
                return View(model);
            }

            // Kiểm tra OTP
            if (user.OtpCode != model.OTP || user.OtpExpiry < DateTime.Now)
            {
                ModelState.AddModelError("OTP", "Mã OTP không đúng hoặc đã hết hạn");
                ViewBag.Type = type;
                return View(model);
            }

            if (type == "register")
            {
                // Hoàn tất đăng ký
                var userId = HttpContext.Session.GetString("RegisterUserId");
                if (string.IsNullOrEmpty(userId) || userId != user.Id.ToString())
                {
                    return RedirectToAction("Register");
                }

                var accountType = HttpContext.Session.GetString("RegisterAccountType");
                var firstName = HttpContext.Session.GetString("RegisterFirstName");
                var lastName = HttpContext.Session.GetString("RegisterLastName");
                var phone = HttpContext.Session.GetString("RegisterPhone");
                var idNo = HttpContext.Session.GetString("RegisterIdNo");
                var dobStr = HttpContext.Session.GetString("RegisterDob");

                if (accountType == "Customer")
                {
                    var customer = new Customer
                    {
                        CustomerId = user.Id,
                        Email = model.Email,
                        FirstName = firstName ?? "",
                        LastName = lastName ?? "",
                        Phone = phone,
                        IdNo = idNo,
                        Dob = DateTime.Parse(dobStr ?? DateTime.Now.ToString("yyyy-MM-dd")),
                        CreatedDate = DateTime.Now
                    };
                    _context.Customers.Add(customer);
                }
                else if (accountType == "Student")
                {
                    var student = new Student
                    {
                        StudentId = user.Id,
                        Email = model.Email,
                        FirstName = firstName ?? "",
                        LastName = lastName ?? "",
                        Phone = phone,
                        IdNo = idNo,
                        Dob = DateTime.Parse(dobStr ?? DateTime.Now.ToString("yyyy-MM-dd")),
                        CreatedDate = DateTime.Now
                    };
                    _context.Students.Add(student);
                }

                // Kích hoạt tài khoản
                user.StatusId = await GetStatusIdAsync("active");
                user.OtpCode = null;
                user.OtpExpiry = null;

                await _context.SaveChangesAsync();

                // Xóa session đăng ký
                HttpContext.Session.Remove("RegisterUserId");
                HttpContext.Session.Remove("RegisterEmail");
                HttpContext.Session.Remove("RegisterAccountType");
                HttpContext.Session.Remove("RegisterFirstName");
                HttpContext.Session.Remove("RegisterLastName");
                HttpContext.Session.Remove("RegisterPhone");
                HttpContext.Session.Remove("RegisterIdNo");
                HttpContext.Session.Remove("RegisterDob");

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            else if (type == "forgotpassword")
            {
                // Chuyển đến trang đổi mật khẩu
                HttpContext.Session.SetString("ResetPasswordEmail", model.Email);
                return RedirectToAction("ResetPassword");
            }

            return RedirectToAction("Login");
        }

        // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            // Trim email
            if (model != null)
            {
                model.Email = model.Email?.Trim() ?? string.Empty;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra email không rỗng sau khi trim
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
                return View(model);
            }

            var emailLower = model.Email.ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
            if (user == null)
            {
                // Không hiển thị lỗi để bảo mật
                TempData["InfoMessage"] = "Nếu email tồn tại, chúng tôi đã gửi mã OTP đến email của bạn.";
                return RedirectToAction("VerifyOTP", new { email = model.Email, type = "forgotpassword" });
            }

            // Tạo và gửi OTP
            var otp = GenerateOTP();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.Now.AddMinutes(10);

            await _context.SaveChangesAsync();

            // Gửi email OTP
            await _emailService.SendOTPEmailAsync(model.Email, otp);

            TempData["InfoMessage"] = "Chúng tôi đã gửi mã OTP đến email của bạn.";
            return RedirectToAction("VerifyOTP", new { email = model.Email, type = "forgotpassword" });
        }

        // GET: Account/ResetPassword
        public IActionResult ResetPassword()
        {
            var email = HttpContext.Session.GetString("ResetPasswordEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email
            };

            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            // Trim các trường
            if (model != null)
            {
                model.Email = model.Email?.Trim() ?? string.Empty;
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra email không rỗng
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email là bắt buộc");
                return View(model);
            }

            // Kiểm tra mật khẩu không rỗng
            if (string.IsNullOrWhiteSpace(model.NewPassword))
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới là bắt buộc");
                return View(model);
            }

            var email = HttpContext.Session.GetString("ResetPasswordEmail");
            if (string.IsNullOrEmpty(email) || email.ToLower() != model.Email.ToLower())
            {
                return RedirectToAction("ForgotPassword");
            }

            var emailLower = model.Email.ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == emailLower);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản");
                return View(model);
            }

            // Cập nhật mật khẩu
            user.PasswordHash = HashPassword(model.NewPassword);
            user.OtpCode = null;
            user.OtpExpiry = null;

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("ResetPasswordEmail");

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Helper methods
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task<long?> GetRoleIdAsync(string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());
            return role?.Id;
        }

        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }
}