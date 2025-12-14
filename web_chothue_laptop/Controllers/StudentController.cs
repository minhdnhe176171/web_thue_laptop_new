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

        // Trang tổng quan - Tất cả laptop
        public async Task<IActionResult> Index(string? search)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToLogin();

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

            var laptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();

            // Đếm số lượng theo từng trạng thái
            await SetLaptopCountsAsync(student.Id);

            ViewBag.CurrentPage = "All";
            return View(laptops);
        }

        // Trang Đang chờ xử lý
        public async Task<IActionResult> Pending(string? search)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToLogin();

            var laptopsQuery = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Where(l => l.StudentId == student.Id && l.Status.StatusName.ToLower() == "pending");

            if (!string.IsNullOrWhiteSpace(search))
            {
                laptopsQuery = laptopsQuery.Where(l => 
                    l.Name.Contains(search) || 
                    (l.Brand != null && l.Brand.BrandName.Contains(search)));
                ViewBag.Search = search;
            }

            var laptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
            await SetLaptopCountsAsync(student.Id);

            ViewBag.CurrentPage = "Pending";
            return View("Index", laptops);
        }

        // Trang Đã phê duyệt
        public async Task<IActionResult> Approved(string? search)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToLogin();

            var laptopsQuery = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Where(l => l.StudentId == student.Id && l.Status.StatusName.ToLower() == "approved");

            if (!string.IsNullOrWhiteSpace(search))
            {
                laptopsQuery = laptopsQuery.Where(l => 
                    l.Name.Contains(search) || 
                    (l.Brand != null && l.Brand.BrandName.Contains(search)));
                ViewBag.Search = search;
            }

            var laptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
            await SetLaptopCountsAsync(student.Id);

            ViewBag.CurrentPage = "Approved";
            return View("Index", laptops);
        }

        // Trang Bị từ chối
        public async Task<IActionResult> Rejected(string? search)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToLogin();

            var laptopsQuery = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Where(l => l.StudentId == student.Id && l.Status.StatusName.ToLower() == "rejected");

            if (!string.IsNullOrWhiteSpace(search))
            {
                laptopsQuery = laptopsQuery.Where(l => 
                    l.Name.Contains(search) || 
                    (l.Brand != null && l.Brand.BrandName.Contains(search)));
                ViewBag.Search = search;
            }

            var laptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
            await SetLaptopCountsAsync(student.Id);

            ViewBag.CurrentPage = "Rejected";
            return View("Index", laptops);
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

            // Trim và validate tên laptop
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                model.Name = model.Name.Trim();
                
                // Kiểm tra độ dài
                if (model.Name.Length < 5)
                {
                    ModelState.AddModelError(nameof(model.Name), "Tên laptop phải có ít nhất 5 ký tự");
                }
                else if (model.Name.Length > 200)
                {
                    ModelState.AddModelError(nameof(model.Name), "Tên laptop không được vượt quá 200 ký tự");
                }
                
                // Kiểm tra format
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Name, @"^[a-zA-Z0-9\s\-_\.]+$"))
                {
                    ModelState.AddModelError(nameof(model.Name), "Tên laptop chỉ được chứa chữ cái, số, dấu cách và ký tự đặc biệt (-_.)");
                }
            }

            // Validate BrandId
            if (model.BrandId == null || model.BrandId <= 0)
            {
                ModelState.AddModelError(nameof(model.BrandId), "Vui lòng chọn hãng laptop");
            }
            else
            {
                // Validate: Tên laptop phải chứa tên hãng
                var brand = await _context.Brands.FindAsync(model.BrandId.Value);
                if (brand != null && !string.IsNullOrWhiteSpace(model.Name))
                {
                    // Kiểm tra tên laptop có chứa tên hãng không (không phân biệt hoa thường)
                    if (!model.Name.ToLower().Contains(brand.BrandName.ToLower()))
                    {
                        ModelState.AddModelError(nameof(model.Name), 
                            $"Tên laptop phải chứa tên hãng '{brand.BrandName}'. Ví dụ: {brand.BrandName} Latitude 5420");
                    }
                }
            }

            // Validate Price
            if (model.Price == null || model.Price < 100000 || model.Price > 1000000)
            {
                ModelState.AddModelError(nameof(model.Price), "Giá phải từ 100,000 đến 1,000,000 VNĐ");
            }

            // Validate deadline
            if (model.Deadline.HasValue && model.Deadline.Value < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.Deadline), "Thời gian đến hạn phải từ hôm nay trở đi");
            }
            else if (!model.Deadline.HasValue)
            {
                ModelState.AddModelError(nameof(model.Deadline), "Vui lòng chọn thời gian đến hạn");
            }

            // Validate thông số kỹ thuật (nếu có)
            if (!string.IsNullOrWhiteSpace(model.Cpu))
            {
                model.Cpu = model.Cpu.Trim();
                if (model.Cpu.Length > 100)
                {
                    ModelState.AddModelError(nameof(model.Cpu), "CPU không được vượt quá 100 ký tự");
                }
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Cpu, @"^[a-zA-Z0-9\s\-]+$"))
                {
                    ModelState.AddModelError(nameof(model.Cpu), "CPU chỉ được chứa chữ cái, số, dấu cách và dấu gạch ngang");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Storage))
            {
                model.Storage = model.Storage.Trim();
                if (model.Storage.Length > 100)
                {
                    ModelState.AddModelError(nameof(model.Storage), "Thông tin lưu trữ không được vượt quá 100 ký tự");
                }
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Storage, @"^[a-zA-Z0-9\s\-]+$"))
                {
                    ModelState.AddModelError(nameof(model.Storage), "Lưu trữ chỉ được chứa chữ cái, số, dấu cách và dấu gạch ngang");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Gpu))
            {
                model.Gpu = model.Gpu.Trim();
                if (model.Gpu.Length > 100)
                {
                    ModelState.AddModelError(nameof(model.Gpu), "GPU không được vượt quá 100 ký tự");
                }
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Gpu, @"^[a-zA-Z0-9\s\-]+$"))
                {
                    ModelState.AddModelError(nameof(model.Gpu), "GPU chỉ được chứa chữ cái, số, dấu cách và dấu gạch ngang");
                }
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
                BrandId = model.BrandId.Value,
                Price = model.Price,
                StudentId = student.Id,
                StatusId = pendingStatusId.Value,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                EndTime = model.Deadline  // Lưu deadline vào ENDTIME thay vì UPDATED_DATE
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
                Deadline = laptop.EndTime,  // Lấy từ ENDTIME thay vì UPDATED_DATE
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

            // Trim và validate tên laptop
            if (!string.IsNullOrWhiteSpace(model.Name))
            {
                model.Name = model.Name.Trim();
                
                if (model.Name.Length < 5)
                {
                    ModelState.AddModelError(nameof(model.Name), "Tên laptop phải có ít nhất 5 ký tự");
                }
                else if (model.Name.Length > 200)
                {
                    ModelState.AddModelError(nameof(model.Name), "Tên laptop không được vượt quá 200 ký tự");
                }
                
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Name, @"^[a-zA-Z0-9\s\-_\.]+$"))
                {
                    ModelState.AddModelError(nameof(model.Name), "Tên laptop chỉ được chứa chữ cái, số, dấu cách và ký tự đặc biệt (-_.)");
                }
            }

            // Validate BrandId và tên laptop phải chứa tên hãng
            if (model.BrandId == null || model.BrandId <= 0)
            {
                ModelState.AddModelError(nameof(model.BrandId), "Vui lòng chọn hãng laptop");
            }
            else
            {
                // Validate: Tên laptop phải chứa tên hãng
                var brand = await _context.Brands.FindAsync(model.BrandId.Value);
                if (brand != null && !string.IsNullOrWhiteSpace(model.Name))
                {
                    // Kiểm tra tên laptop có chứa tên hãng không (không phân biệt hoa thường)
                    if (!model.Name.ToLower().Contains(brand.BrandName.ToLower()))
                    {
                        ModelState.AddModelError(nameof(model.Name), 
                            $"Tên laptop phải chứa tên hãng '{brand.BrandName}'. Ví dụ: {brand.BrandName} Latitude 5420");
                    }
                }
            }

            // Validate deadline
            if (model.Deadline.HasValue && model.Deadline.Value < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.Deadline), "Thời gian đến hạn phải từ hôm nay trở đi");
            }

            // Validate thông số kỹ thuật
            if (!string.IsNullOrWhiteSpace(model.Cpu))
            {
                model.Cpu = model.Cpu.Trim();
                if (model.Cpu.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(model.Cpu, @"^[a-zA-Z0-9\s\-]+$"))
                {
                    ModelState.AddModelError(nameof(model.Cpu), "CPU không hợp lệ");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Storage))
            {
                model.Storage = model.Storage.Trim();
                if (model.Storage.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(model.Storage, @"^[a-zA-Z0-9\s\-]+$"))
                {
                    ModelState.AddModelError(nameof(model.Storage), "Thông tin lưu trữ không hợp lệ");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.Gpu))
            {
                model.Gpu = model.Gpu.Trim();
                if (model.Gpu.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(model.Gpu, @"^[a-zA-Z0-9\s\-]+$"))
                {
                    ModelState.AddModelError(nameof(model.Gpu), "GPU không hợp lệ");
                }
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
            laptop.BrandId = model.BrandId.Value;
            laptop.Price = model.Price;
            laptop.UpdatedDate = DateTime.Now;  // Cập nhật thời gian sửa
            laptop.EndTime = model.Deadline;    // Lưu deadline vào ENDTIME

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

        public async Task<IActionResult> Report()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem báo cáo.";
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

            // Lấy tất cả laptop của student
            var laptops = await _context.Laptops
                .Where(l => l.StudentId == student.Id)
                .ToListAsync();

            var laptopIds = laptops.Select(l => l.Id).ToList();

            // Tính tổng thu nhập từ các booking đã hoàn thành
            var totalIncome = await _context.BookingReceipts
                .Include(br => br.Booking)
                .Where(br => laptopIds.Contains(br.Booking.LaptopId))
                .SumAsync(br => br.TotalPrice);

            ViewBag.Income = totalIncome.ToString("#,##0") + " VNĐ";

            // Lấy danh sách booking đã hoàn thành (có BookingReceipt)
            var completedBookings = await _context.BookingReceipts
                .Include(br => br.Booking)
                    .ThenInclude(b => b.Laptop)
                        .ThenInclude(l => l.Brand)
                .Include(br => br.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(br => br.Booking)
                    .ThenInclude(b => b.Status)
                .Where(br => laptopIds.Contains(br.Booking.LaptopId))
                .OrderByDescending(br => br.CreatedDate)
                .ToListAsync();

            ViewBag.CompletedBookings = completedBookings;

            // Lấy danh sách booking đang thuê (status = Rented)
            var rentedBookings = await _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Customer)
                .Include(b => b.Status)
                .Where(b => laptopIds.Contains(b.LaptopId) && 
                           b.Status.StatusName.ToLower() == "rented")
                .OrderBy(b => b.EndTime)
                .ToListAsync();

            ViewBag.RentedBookings = rentedBookings;

            // Thống kê
            ViewBag.TotalBookings = completedBookings.Count + rentedBookings.Count;
            ViewBag.TotalRented = rentedBookings.Count;
            ViewBag.TotalCompleted = completedBookings.Count;

            // Lấy danh sách Technical Tickets liên quan đến laptop của student
            var tickets = await _context.TechnicalTickets
                .Include(t => t.Laptop)
                .Include(t => t.Status)
                .Where(t => laptopIds.Contains(t.LaptopId))
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            return View(tickets);
        }

        // Helper Methods
        private async Task<Student?> GetCurrentStudentAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return null;

            var userIdLong = long.Parse(userId);
            return await _context.Students.FirstOrDefaultAsync(s => s.StudentId == userIdLong);
        }

        private IActionResult RedirectToLogin()
        {
            TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục.";
            return RedirectToAction("Login", "Account");
        }

        private async Task SetLaptopCountsAsync(long studentId)
        {
            var allLaptops = await _context.Laptops
                .Include(l => l.Status)
                .Where(l => l.StudentId == studentId)
                .ToListAsync();

            ViewBag.PendingCount = allLaptops.Count(l => l.Status?.StatusName?.ToLower() == "pending");
            ViewBag.ApprovedCount = allLaptops.Count(l => l.Status?.StatusName?.ToLower() == "approved");
            ViewBag.RejectedCount = allLaptops.Count(l => l.Status?.StatusName?.ToLower() == "rejected");
            ViewBag.TotalCount = allLaptops.Count;
        }

        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }
}