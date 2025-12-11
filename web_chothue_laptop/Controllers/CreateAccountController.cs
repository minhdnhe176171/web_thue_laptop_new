using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Controllers
{
    public class CreateAccountController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<CreateAccountController> _logger;

        public CreateAccountController(Swp391LaptopContext context, ILogger<CreateAccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CreateAccount
        public IActionResult Index()
        {
            return View("~/Views/Admin/CreateAccount.cshtml", new CreateAccountViewModel());
        }

        // POST: CreateAccount/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/CreateAccount.cshtml", model);
            }

            try
            {
                // Validate email uniqueness in User table
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng chọn email khác.");
                    return View("~/Views/Admin/CreateAccount.cshtml", model);
                }

                // Validate email uniqueness in specific account type tables
                bool emailExists = false;
                switch (model.AccountType.ToLower())
                {
                    case "staff":
                        emailExists = await _context.Staff.AnyAsync(s => s.Email == model.Email);
                        break;
                    case "technical":
                        emailExists = await _context.Technicals.AnyAsync(t => t.Email == model.Email);
                        break;
                    case "manager":
                        emailExists = await _context.Managers.AnyAsync(m => m.Email == model.Email);
                        break;
                }
                
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng trong hệ thống. Vui lòng chọn email khác.");
                    return View("~/Views/Admin/CreateAccount.cshtml", model);
                }

                // Validate password match
                if (model.Password != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                    return View("~/Views/Admin/CreateAccount.cshtml", model);
                }

                // Get Role ID based on account type
                long roleId = await GetRoleIdAsync(model.AccountType);
                if (roleId == 0)
                {
                    // Try to get all available roles for debugging
                    List<string> availableRoles = new List<string>();
                    try
                    {
                        availableRoles = await _context.Roles.Select(r => r.RoleName).ToListAsync();
                        _logger.LogWarning("Role ID is 0 for account type: {AccountType}. Available roles: {Roles}", 
                            model.AccountType, string.Join(", ", availableRoles));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading roles from database");
                        availableRoles.Add("Lỗi khi tải danh sách roles từ database");
                    }
                    
                    string rolesMessage = availableRoles.Any() 
                        ? string.Join(", ", availableRoles) 
                        : "Không có role nào trong database. Vui lòng kiểm tra dữ liệu.";
                    
                    ModelState.AddModelError("AccountType", 
                        $"Loại tài khoản không hợp lệ. Vui lòng kiểm tra lại. (Các role có sẵn: {rolesMessage})");
                    return View("~/Views/Admin/CreateAccount.cshtml", model);
                }

                // Get active status ID dynamically (same as AccountController)
                long? statusIdNullable = await GetStatusIdAsync("active");
                if (statusIdNullable == null || statusIdNullable.Value == 0)
                {
                    ModelState.AddModelError("", "Không tìm thấy trạng thái 'active' trong hệ thống. Vui lòng liên hệ quản trị viên.");
                    return View("~/Views/Admin/CreateAccount.cshtml", model);
                }
                long statusId = statusIdNullable.Value;

                // Hash password using SHA256 (same as AccountController)
                string passwordHash = HashPassword(model.Password);

                // Create User
                var user = new User
                {
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    RoleId = roleId,
                    StatusId = statusId,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create specific account type record
                switch (model.AccountType.ToLower())
                {
                    case "staff":
                        var staff = new Staff
                        {
                            StaffId = user.Id,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Email = model.Email,
                            Phone = model.Phone,
                            IdNo = model.IdNo,
                            Dob = model.Dob,
                            CreatedDate = DateTime.Now
                        };
                        _context.Staff.Add(staff);
                        break;

                    case "technical":
                        var technical = new Technical
                        {
                            TechnicalId = user.Id,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Email = model.Email,
                            Phone = model.Phone,
                            IdNo = model.IdNo,
                            Dob = model.Dob,
                            CreatedDate = DateTime.Now
                        };
                        _context.Technicals.Add(technical);
                        break;

                    case "manager":
                        var manager = new Manager
                        {
                            ManagerId = user.Id,
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            Email = model.Email,
                            Phone = model.Phone,
                            IdNo = model.IdNo,
                            Dob = model.Dob,
                            CreatedDate = DateTime.Now
                        };
                        _context.Managers.Add(manager);
                        break;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Tạo tài khoản {model.AccountType} thành công!";
                return RedirectToAction("Index");
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error creating account");
                string errorMessage = "Đã xảy ra lỗi khi tạo tài khoản.";
                
                // Check for specific SQL errors
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601) // Unique constraint violation
                {
                    if (sqlEx.Message.Contains("EMAIL"))
                    {
                        errorMessage = "Email này đã được sử dụng. Vui lòng chọn email khác.";
                        ModelState.AddModelError("Email", errorMessage);
                    }
                    else
                    {
                        errorMessage = "Dữ liệu đã tồn tại trong hệ thống.";
                    }
                }
                else
                {
                    errorMessage = $"Lỗi SQL Server: {sqlEx.Message}";
                }
                
                ModelState.AddModelError("", errorMessage);
                return View("~/Views/Admin/CreateAccount.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account: {Message}", ex.Message);
                ModelState.AddModelError("", $"Đã xảy ra lỗi khi tạo tài khoản: {ex.Message}");
                return View("~/Views/Admin/CreateAccount.cshtml", model);
            }
        }

        private async Task<long> GetRoleIdAsync(string accountType)
        {
            try
            {
                // First, check if Roles table has any data
                var rolesCount = await _context.Roles.CountAsync();
                if (rolesCount == 0)
                {
                    _logger.LogWarning("Roles table is empty. No roles found in database.");
                    return 0;
                }

                // Get all roles first for logging
                var allRoles = await _context.Roles.Select(r => r.RoleName).ToListAsync();
                _logger.LogInformation("Available roles in database: {Roles}", string.Join(", ", allRoles));

                // Try exact match first (case-insensitive)
                var role = await _context.Roles.FirstOrDefaultAsync(r => 
                    r.RoleName.ToLower() == accountType.ToLower());
                
                // If not found, try partial matching
                if (role == null)
                {
                    role = await _context.Roles.FirstOrDefaultAsync(r => 
                        r.RoleName.ToLower().Contains(accountType.ToLower()) ||
                        accountType.ToLower().Contains(r.RoleName.ToLower()));
                }
                
                if (role == null)
                {
                    _logger.LogWarning("Role not found for account type: {AccountType}. Available roles: {Roles}", 
                        accountType, string.Join(", ", allRoles));
                }
                else
                {
                    _logger.LogInformation("Found role: {RoleName} (ID: {RoleId}) for account type: {AccountType}", 
                        role.RoleName, role.Id, accountType);
                }
                
                return role?.Id ?? 0;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL error getting role ID for account type: {AccountType}. Error: {Message}", 
                    accountType, sqlEx.Message);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role ID for account type: {AccountType}. Error: {Message}", 
                    accountType, ex.Message);
                return 0;
            }
        }

        // Helper method to hash password (same as AccountController)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        // Helper method to get status ID by name (same as AccountController)
        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }

    public class CreateAccountViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn loại tài khoản")]
        public string AccountType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ")]
        [Display(Name = "Họ")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên")]
        [Display(Name = "Tên")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Số CMND/CCCD")]
        public string? IdNo { get; set; }

        [Display(Name = "Ngày sinh")]
        public DateTime? Dob { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

