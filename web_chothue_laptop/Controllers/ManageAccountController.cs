using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Controllers
{
    public class ManageAccountController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<ManageAccountController> _logger;

        public ManageAccountController(Swp391LaptopContext context, ILogger<ManageAccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: ManageAccount - Redirect to Student by default
        public IActionResult Index()
        {
            return RedirectToAction("Student");
        }

        // GET: ManageAccount/Student
        public async Task<IActionResult> Student(string? filterStatus, string? searchAccount, int page = 1)
        {
            return await LoadAccountsByRole("student", filterStatus, searchAccount, page);
        }

        // GET: ManageAccount/Customer
        public async Task<IActionResult> Customer(string? filterStatus, string? searchAccount, int page = 1)
        {
            return await LoadAccountsByRole("customer", filterStatus, searchAccount, page);
        }

        // GET: ManageAccount/Staff
        public async Task<IActionResult> Staff(string? filterStatus, string? searchAccount, int page = 1)
        {
            return await LoadAccountsByRole("staff", filterStatus, searchAccount, page);
        }

        // GET: ManageAccount/Manager
        public async Task<IActionResult> Manager(string? filterStatus, string? searchAccount, int page = 1)
        {
            return await LoadAccountsByRole("manager", filterStatus, searchAccount, page);
        }

        // GET: ManageAccount/Technical
        public async Task<IActionResult> Technical(string? filterStatus, string? searchAccount, int page = 1)
        {
            return await LoadAccountsByRole("technical", filterStatus, searchAccount, page);
        }

        // Helper method to load accounts by role
        private async Task<IActionResult> LoadAccountsByRole(string roleName, string? filterStatus, string? searchAccount, int page = 1)
        {
            try
            {
                // Load all Staff, Technical, Manager, Student, Customer data first to avoid multiple queries
                var allStaff = _context.Staff.ToList();
                var allTechnicals = _context.Technicals.ToList();
                var allManagers = _context.Managers.ToList();
                var allStudents = _context.Students.ToList();
                var allCustomers = _context.Customers.ToList();

                var query = from u in _context.Users
                            join r in _context.Roles on u.RoleId equals r.Id into roleGroup
                            from role in roleGroup.DefaultIfEmpty()
                            join s in _context.Statuses on u.StatusId equals s.Id into statusGroup
                            from status in statusGroup.DefaultIfEmpty()
                            where role != null && role.RoleName.ToLower() == roleName.ToLower()
                            select new
                            {
                                UserId = u.Id,
                                Email = u.Email,
                                RoleName = role != null ? role.RoleName : "N/A",
                                StatusName = status != null ? status.StatusName : "N/A",
                                StatusId = u.StatusId ?? 0,
                                CreatedDate = u.CreatedDate ?? DateTime.Now,
                                RoleId = u.RoleId
                            };

                if (!string.IsNullOrEmpty(filterStatus))
                {
                    if (filterStatus == "active")
                    {
                        // Chỉ lọc tài khoản đang hoạt động (StatusId == 6)
                        query = query.Where(x => x.StatusId == 6);
                    }
                    else if (filterStatus == "locked")
                    {
                        // Chỉ lọc tài khoản đã khóa (StatusId == 7)
                        query = query.Where(x => x.StatusId == 7);
                    }
                }

                if (!string.IsNullOrEmpty(searchAccount))
                {
                    query = query.Where(x => x.Email.Contains(searchAccount));
                }

                // Get all data first (before pagination)
                var data = await query
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                // Convert to ViewModel and get FullName using in-memory data
                var allAccounts = data.Select(x => new AccountViewModel
                {
                    UserId = x.UserId,
                    Email = x.Email,
                    RoleName = x.RoleName,
                    StatusName = x.StatusName,
                    StatusId = x.StatusId,
                    CreatedDate = x.CreatedDate,
                    FullName = GetFullNameFromMemory(x.UserId, x.RoleName, allStaff, allTechnicals, allManagers, allStudents, allCustomers)
                }).ToList();

                // Apply search filter on FullName after loading
                if (!string.IsNullOrEmpty(searchAccount))
                {
                    allAccounts = allAccounts.Where(x =>
                        x.FullName.Contains(searchAccount, StringComparison.OrdinalIgnoreCase) ||
                        x.Email.Contains(searchAccount, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // Pagination after filtering
                int pageSize = 10;
                int totalItems = allAccounts.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var accounts = allAccounts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentRole = roleName;
                ViewBag.FilterStatus = filterStatus;
                ViewBag.SearchAccount = searchAccount;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                return View("~/Views/Admin/ManageAccount.cshtml", accounts);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Server connection error");
                ViewBag.ErrorMessage = $"Lỗi kết nối SQL Server: {sqlEx.Message}. Vui lòng kiểm tra:\n" +
                    "- SQL Server có đang chạy không?\n" +
                    "- Connection string có đúng không?\n" +
                    "- Server name: DESKTOP-0NF6T35\n" +
                    "- Database: swp391_laptop";
                return View("~/Views/Admin/ManageAccount.cshtml", new List<AccountViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading accounts");
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}. Vui lòng kiểm tra kết nối SQL Server.";
                return View("~/Views/Admin/ManageAccount.cshtml", new List<AccountViewModel>());
            }
        }

        // POST: ManageAccount/ToggleStatus
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleStatus(long userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tài khoản." });
                }

                // Toggle status: if active (6) then lock (7), else unlock (6)
                // Status ID 6 = Active (Đang hoạt động), 7 = Inactive (Đã khóa)
                if (user.StatusId == 6)
                {
                    user.StatusId = 7; // Lock (Inactive)
                }
                else
                {
                    user.StatusId = 6; // Unlock (Active)
                }

                await _context.SaveChangesAsync();

                var statusName = user.StatusId == 6 ? "mở khóa" : "khóa";
                return Json(new { success = true, message = $"Đã {statusName} tài khoản thành công.", statusId = user.StatusId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling account status");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        private string GetFullNameFromMemory(long userId, string roleName,
            List<Staff> allStaff, List<Technical> allTechnicals, List<Manager> allManagers,
            List<Student> allStudents, List<Customer> allCustomers)
        {
            try
            {
                switch (roleName.ToLower())
                {
                    case "staff":
                        var staff = allStaff.FirstOrDefault(s => s.StaffId == userId);
                        return staff != null ? $"{staff.LastName} {staff.FirstName}" : "N/A";
                    case "technical":
                        var technical = allTechnicals.FirstOrDefault(t => t.TechnicalId == userId);
                        return technical != null ? $"{technical.LastName} {technical.FirstName}" : "N/A";
                    case "manager":
                        var manager = allManagers.FirstOrDefault(m => m.ManagerId == userId);
                        return manager != null ? $"{manager.LastName} {manager.FirstName}" : "N/A";
                    case "student":
                        var student = allStudents.FirstOrDefault(s => s.StudentId == userId);
                        return student != null ? $"{student.LastName} {student.FirstName}" : "N/A";
                    case "customer":
                        var customer = allCustomers.FirstOrDefault(c => c.CustomerId == userId);
                        return customer != null ? $"{customer.LastName} {customer.FirstName}" : "N/A";
                    default:
                        return "N/A";
                }
            }
            catch
            {
                return "N/A";
            }
        }
    }

    public class AccountViewModel
    {
        public long UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public long StatusId { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}