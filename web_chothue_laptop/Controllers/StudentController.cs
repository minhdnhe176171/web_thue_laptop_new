using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.ViewModels;

namespace web_chothue_laptop.Controllers
{
    public class StudentController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<StudentController> _logger;

        public StudentController(Swp391LaptopContext context, ILogger<StudentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem danh sách laptop.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var laptopsQuery = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Where(l => l.StudentId == student.Id);

            // Lọc theo search
            if (!string.IsNullOrWhiteSpace(search))
            {
                laptopsQuery = laptopsQuery.Where(l => 
                    l.Name.Contains(search) || 
                    (l.Brand != null && l.Brand.BrandName.Contains(search)));
                ViewBag.Search = search;
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusLower = status.ToLower();
                laptopsQuery = laptopsQuery.Where(l => 
                    l.Status != null && l.Status.StatusName.ToLower() == statusLower);
                ViewBag.CurrentStatus = status;
            }

            var laptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();

            // Đếm số lượng theo từng trạng thái
            var allLaptops = await _context.Laptops
                .Include(l => l.Status)
                .Where(l => l.StudentId == student.Id)
                .ToListAsync();

            ViewBag.PendingCount = allLaptops.Count(l => l.Status?.StatusName?.ToLower() == "pending");
            ViewBag.ApprovedCount = allLaptops.Count(l => l.Status?.StatusName?.ToLower() == "approved");
            ViewBag.RejectedCount = allLaptops.Count(l => l.Status?.StatusName?.ToLower() == "rejected");
            ViewBag.TotalCount = allLaptops.Count;

            return View(laptops);
        }

        public async Task<IActionResult> Create()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tạo laptop.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Brands = await _context.Brands.ToListAsync();
            
            // Set deadline mặc định là 30 ngày sau
            var model = new CreateLaptopViewModel
            {
                Deadline = DateTime.Today.AddDays(30)
            };
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateLaptopViewModel model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tạo laptop.";
                return RedirectToAction("Login", "Account");
            }

            // Validate deadline
            if (model.Deadline.HasValue && model.Deadline.Value < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.Deadline), "Thời gian đến hạn phải từ hôm nay trở đi");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Brands = await _context.Brands.ToListAsync();
                return View(model);
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var pendingStatusId = await GetStatusIdAsync("pending");
            if (pendingStatusId == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Vui lòng thử lại sau.";
                ViewBag.Brands = await _context.Brands.ToListAsync();
                return View(model);
            }

            var laptop = new Laptop
            {
                Name = model.Name,
                BrandId = model.BrandId,
                Price = model.Price,
                StudentId = student.Id,
                StatusId = pendingStatusId.Value,
                CreatedDate = DateTime.Now, // Thời gian tạo
                UpdatedDate = model.Deadline // Thời gian đến hạn
            };

            _context.Laptops.Add(laptop);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(model.Cpu) || 
                !string.IsNullOrWhiteSpace(model.RamSize) || 
                !string.IsNullOrWhiteSpace(model.Storage) || 
                !string.IsNullOrWhiteSpace(model.Gpu))
            {
                var laptopDetail = new LaptopDetail
                {
                    LaptopId = laptop.Id,
                    Cpu = model.Cpu,
                    RamSize = model.RamSize,
                    Storage = model.Storage,
                    Gpu = model.Gpu
                };

                _context.LaptopDetails.Add(laptopDetail);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Tạo laptop thành công! Đang chờ phê duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // Thêm action Details để xem chi tiết
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem chi tiết laptop.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var laptop = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.LaptopDetails)
                .Include(l => l.Student)
                .FirstOrDefaultAsync(l => l.Id == id && l.StudentId == student.Id);

            if (laptop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy laptop hoặc bạn không có quyền xem laptop này.";
                return RedirectToAction(nameof(Index));
            }

            return View(laptop);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để chỉnh sửa laptop.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var laptop = await _context.Laptops
                .Include(l => l.LaptopDetails)
                .FirstOrDefaultAsync(l => l.Id == id && l.StudentId == student.Id);

            if (laptop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy laptop hoặc bạn không có quyền chỉnh sửa laptop này.";
                return RedirectToAction(nameof(Index));
            }

            var laptopDetail = laptop.LaptopDetails.FirstOrDefault();
            var model = new CreateLaptopViewModel
            {
                Id = laptop.Id,
                Name = laptop.Name,
                BrandId = laptop.BrandId,
                Price = laptop.Price,
                Deadline = laptop.UpdatedDate,
                Cpu = laptopDetail?.Cpu,
                RamSize = laptopDetail?.RamSize,
                Storage = laptopDetail?.Storage,
                Gpu = laptopDetail?.Gpu
            };

            ViewBag.Brands = await _context.Brands.ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateLaptopViewModel model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để chỉnh sửa laptop.";
                return RedirectToAction("Login", "Account");
            }

            // Validate deadline
            if (model.Deadline.HasValue && model.Deadline.Value < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.Deadline), "Thời gian đến hạn phải từ hôm nay trở đi");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Brands = await _context.Brands.ToListAsync();
                return View(model);
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var laptop = await _context.Laptops
                .Include(l => l.LaptopDetails)
                .FirstOrDefaultAsync(l => l.Id == model.Id && l.StudentId == student.Id);

            if (laptop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy laptop hoặc bạn không có quyền chỉnh sửa laptop này.";
                ViewBag.Brands = await _context.Brands.ToListAsync();
                return View(model);
            }

            laptop.Name = model.Name;
            laptop.BrandId = model.BrandId;
            laptop.Price = model.Price;
            laptop.UpdatedDate = model.Deadline; // Cập nhật deadline

            var laptopDetail = laptop.LaptopDetails.FirstOrDefault();
            if (laptopDetail != null)
            {
                laptopDetail.Cpu = model.Cpu;
                laptopDetail.RamSize = model.RamSize;
                laptopDetail.Storage = model.Storage;
                laptopDetail.Gpu = model.Gpu;
            }
            else if (!string.IsNullOrWhiteSpace(model.Cpu) || 
                     !string.IsNullOrWhiteSpace(model.RamSize) || 
                     !string.IsNullOrWhiteSpace(model.Storage) || 
                     !string.IsNullOrWhiteSpace(model.Gpu))
            {
                var newDetail = new LaptopDetail
                {
                    LaptopId = laptop.Id,
                    Cpu = model.Cpu,
                    RamSize = model.RamSize,
                    Storage = model.Storage,
                    Gpu = model.Gpu
                };
                _context.LaptopDetails.Add(newDetail);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật laptop thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xóa laptop.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            var laptop = await _context.Laptops
                .Include(l => l.LaptopDetails)
                .Include(l => l.Bookings)
                .FirstOrDefaultAsync(l => l.Id == id && l.StudentId == student.Id);

            if (laptop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy laptop hoặc bạn không có quyền xóa laptop này.";
                return RedirectToAction(nameof(Index));
            }

            if (laptop.Bookings.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa laptop đã có đơn đặt thuê.";
                return RedirectToAction(nameof(Index));
            }

            if (laptop.LaptopDetails.Any())
            {
                _context.LaptopDetails.RemoveRange(laptop.LaptopDetails);
            }

            _context.Laptops.Remove(laptop);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa laptop thành công!";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Report()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem báo cáo.";
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }
}