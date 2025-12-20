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
        public async Task<IActionResult> Index(string? search, string? fromDate, string? toDate, int page = 1)
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

            // Lọc theo ngày tạo
            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var from))
            {
                laptopsQuery = laptopsQuery.Where(l => l.CreatedDate >= from);
                ViewBag.FromDate = fromDate;
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var to))
            {
                // Thêm 1 ngày để bao gồm cả ngày cuối
                var toDateEnd = to.AddDays(1);
                laptopsQuery = laptopsQuery.Where(l => l.CreatedDate < toDateEnd);
                ViewBag.ToDate = toDate;
            }

            var laptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();

            // Đếm số lượng theo từng trạng thái
            await SetLaptopCountsAsync(student.Id);

            ViewBag.CurrentPage = "All";
            ViewBag.Page = page;
            return View(laptops);
        }

        // Trang Đang chờ xử lý
        public async Task<IActionResult> Pending(string? search, string? fromDate, string? toDate, int page = 1)
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

            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var from))
            {
                laptopsQuery = laptopsQuery.Where(l => l.CreatedDate >= from);
                ViewBag.FromDate = fromDate;
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var to))
            {
                var toDateEnd = to.AddDays(1);
                laptopsQuery = laptopsQuery.Where(l => l.CreatedDate < toDateEnd);
                ViewBag.ToDate = toDate;
            }

            var laptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
            await SetLaptopCountsAsync(student.Id);

            ViewBag.CurrentPage = "Pending";
            ViewBag.Page = page;
            return View("Index", laptops);
        }

        // Trang Đã phê duyệt
        public async Task<IActionResult> Approved(string? search, string? fromDate, string? toDate, int page = 1)
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

            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var from))
            {
                laptopsQuery = laptopsQuery.Where(l => l.CreatedDate >= from);
                ViewBag.FromDate = fromDate;
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var to))
            {
                var toDateEnd = to.AddDays(1);
                laptopsQuery = laptopsQuery.Where(l => l.CreatedDate < toDateEnd);
                ViewBag.ToDate = toDate;
            }

            var laptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
            await SetLaptopCountsAsync(student.Id);

            ViewBag.CurrentPage = "Approved";
            ViewBag.Page = page;
            return View("Index", laptops);
        }

        // Trang Bị từ chối - Hiển thị Laptop bị từ chối HOẶC đang sửa
        public async Task<IActionResult> Rejected(string? search, string? fromDate, string? toDate, int page = 1)
        {
            var student = await GetCurrentStudentAsync();
            if (student == null) return RedirectToLogin();

            var laptopsQuery = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.TechnicalTickets)
                .Where(l => l.StudentId == student.Id && 
                           (l.Status.StatusName.ToLower() == "rejected" || 
                            l.Status.StatusName.ToLower() == "fixing"));

            if (!string.IsNullOrWhiteSpace(search))
            {
                laptopsQuery = laptopsQuery.Where(l => 
                    l.Name.Contains(search) || 
                    (l.Brand != null && l.Brand.BrandName.Contains(search)));
                ViewBag.Search = search;
            }

            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var from))
            {
                laptopsQuery = laptopsQuery.Where(l => l.CreatedDate >= from);
                ViewBag.FromDate = fromDate;
            }

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var to))
            {
                var toDateEnd = to.AddDays(1);
                laptopsQuery = laptopsQuery.Where(l => l.CreatedDate < toDateEnd);
                ViewBag.ToDate = toDate;
            }

            var rejectedLaptops = await laptopsQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
            
            // Đếm số lượng
            await SetLaptopCountsAsync(student.Id);

            ViewBag.CurrentPage = "Rejected";
            ViewBag.Page = page;
            return View(rejectedLaptops);
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
            if (model.Deadline.HasValue)
            {
                if (model.Deadline.Value < DateTime.Today)
                {
                    ModelState.AddModelError(nameof(model.Deadline), "Thời gian đến hạn phải từ hôm nay trở đi");
                }
                else if (model.Deadline.Value < DateTime.Today.AddDays(5))
                {
                    ModelState.AddModelError(nameof(model.Deadline), "Thời gian đến hạn phải ít nhất 5 ngày kể từ hôm nay");
                }
            }
            else
            {
                ModelState.AddModelError(nameof(model.Deadline), "Vui lòng chọn thời gian đến hạn");
            }

            // Validate image file nếu có
            if (model.ImageFile != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(model.ImageFile), "Chỉ chấp nhận file ảnh (JPG, PNG, GIF)");
                }
                else if (model.ImageFile.Length > 5 * 1024 * 1024) // 5MB
                {
                    ModelState.AddModelError(nameof(model.ImageFile), "Kích thước ảnh không được vượt quá 5MB");
                }
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

            // Upload ảnh nếu có
            string? imageUrl = null;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "laptops");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.ImageFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    imageUrl = $"/images/laptops/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading image");
                    ModelState.AddModelError(nameof(model.ImageFile), "Có lỗi khi upload ảnh. Vui lòng thử lại.");
                    ViewBag.Brands = await _context.Brands.ToListAsync();
                    return View(model);
                }
            }

            var laptop = new Laptop
            {
                Name = model.Name,
                BrandId = model.BrandId!.Value,
                Price = model.Price,
                StudentId = student.Id,
                StatusId = pendingStatusId.Value,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                EndTime = model.Deadline,
                ImageUrl = imageUrl
            };

            _context.Laptops.Add(laptop);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(model.Cpu) || 
                !string.IsNullOrWhiteSpace(model.RamSize) || 
                !string.IsNullOrWhiteSpace(model.RamType) ||
                !string.IsNullOrWhiteSpace(model.Storage) || 
                !string.IsNullOrWhiteSpace(model.Gpu) ||
                !string.IsNullOrWhiteSpace(model.ScreenSize) ||
                !string.IsNullOrWhiteSpace(model.Os))
            {
                var laptopDetail = new LaptopDetail
                {
                    LaptopId = laptop.Id,
                    Cpu = model.Cpu,
                    RamSize = model.RamSize,
                    RamType = model.RamType,
                    Storage = model.Storage,
                    Gpu = model.Gpu,
                    ScreenSize = model.ScreenSize,
                    Os = model.Os
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
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Customer)
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Status)
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
                RamType = laptopDetail?.RamType,
                Storage = laptopDetail?.Storage,
                Gpu = laptopDetail?.Gpu,
                ScreenSize = laptopDetail?.ScreenSize,
                Os = laptopDetail?.Os
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
            if (model.Deadline.HasValue)
            {
                if (model.Deadline.Value < DateTime.Today)
                {
                    ModelState.AddModelError(nameof(model.Deadline), "Thời gian đến hạn phải từ hôm nay trở đi");
                }
                else if (model.Deadline.Value < DateTime.Today.AddDays(5))
                {
                    ModelState.AddModelError(nameof(model.Deadline), "Thời gian đến hạn phải ít nhất 5 ngày kể từ hôm nay");
                }
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
            laptop.BrandId = model.BrandId;
            laptop.Price = model.Price;
            laptop.UpdatedDate = DateTime.Now;  // Cập nhật thời gian sửa
            laptop.EndTime = model.Deadline;    // Lưu deadline vào ENDTIME

            var laptopDetail = laptop.LaptopDetails.FirstOrDefault();
            if (laptopDetail != null)
            {
                laptopDetail.Cpu = model.Cpu;
                laptopDetail.RamSize = model.RamSize;
                laptopDetail.RamType = model.RamType;
                laptopDetail.Storage = model.Storage;
                laptopDetail.Gpu = model.Gpu;
                laptopDetail.ScreenSize = model.ScreenSize;
                laptopDetail.Os = model.Os;
            }
            else if (!string.IsNullOrWhiteSpace(model.Cpu) || 
                     !string.IsNullOrWhiteSpace(model.RamSize) || 
                     !string.IsNullOrWhiteSpace(model.RamType) ||
                     !string.IsNullOrWhiteSpace(model.Storage) || 
                     !string.IsNullOrWhiteSpace(model.Gpu) ||
                     !string.IsNullOrWhiteSpace(model.ScreenSize) ||
                     !string.IsNullOrWhiteSpace(model.Os))
            {
                var newDetail = new LaptopDetail
                {
                    LaptopId = laptop.Id,
                    Cpu = model.Cpu,
                    RamSize = model.RamSize,
                    RamType = model.RamType,
                    Storage = model.Storage,
                    Gpu = model.Gpu,
                    ScreenSize = model.ScreenSize,
                    Os = model.Os
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

        public async Task<IActionResult> Report(string? search, int page = 1)
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

            // Lấy danh sách BookingReceipts đã hoàn thành
            var completedQuery = _context.BookingReceipts
                .Include(br => br.Booking)
                    .ThenInclude(b => b.Laptop)
                        .ThenInclude(l => l.Brand)
                .Include(br => br.Booking)
                    .ThenInclude(b => b.Customer)
                .Where(br => laptopIds.Contains(br.Booking.LaptopId))
                .AsQueryable();

            // Tìm kiếm theo tên laptop
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                completedQuery = completedQuery.Where(br =>
                    (br.Booking.Laptop != null && br.Booking.Laptop.Name.ToLower().Contains(search)) ||
                    (br.Booking.Laptop != null && br.Booking.Laptop.Brand != null && br.Booking.Laptop.Brand.BrandName.ToLower().Contains(search))
                );
                ViewBag.Search = search;
            }

            var completedBookings = await completedQuery
                .OrderByDescending(br => br.CreatedDate)
                .ToListAsync();

            ViewBag.CurrentPage = page;

            return View(completedBookings);
        }

        // Action mới: Hiển thị laptop đang cho thuê
        public async Task<IActionResult> MyRentals(string? tab, int page = 1)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem danh sách cho thuê.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy tất cả laptop của student với bookings
            var myLaptops = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.LaptopDetails)
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Customer)
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Status)
                .Where(l => l.StudentId == student.Id && l.Status.StatusName.ToLower() == "available")
                .OrderByDescending(l => l.CreatedDate)
                .ToListAsync();

            // Set pagination info based on tab
            if (tab == "rented")
            {
                ViewBag.RentedPage = page;
                ViewBag.AvailablePage = 1;
                ViewBag.OverduePage = 1;
            }
            else if (tab == "overdue")
            {
                ViewBag.OverduePage = page;
                ViewBag.AvailablePage = 1;
                ViewBag.RentedPage = 1;
            }
            else
            {
                ViewBag.AvailablePage = page;
                ViewBag.RentedPage = 1;
                ViewBag.OverduePage = 1;
            }

            return View(myLaptops);
        }

        // Action: Trang thông báo trả máy
        public async Task<IActionResult> Notifications(string? search)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông báo.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy các laptop có booking Close (StatusId = 8) với thông báo trả máy
            var query = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.LaptopDetails)
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Customer)
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Status)
                .Where(l => l.StudentId == student.Id && 
                           l.Bookings.Any(b => b.StatusId == 8 && 
                                              !string.IsNullOrEmpty(b.RejectReason) && 
                                              b.RejectReason.StartsWith("RETURN_SCHEDULE|")));

            // Tìm kiếm theo tên laptop hoặc khách hàng
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(l => 
                    l.Name.ToLower().Contains(search) ||
                    (l.Brand != null && l.Brand.BrandName.ToLower().Contains(search)) ||
                    l.Bookings.Any(b => 
                        b.StatusId == 8 &&
                        (b.Customer != null && (
                            b.Customer.FirstName.ToLower().Contains(search) ||
                            b.Customer.LastName.ToLower().Contains(search) ||
                            b.Customer.Email.ToLower().Contains(search) ||
                            (b.Customer.FirstName + " " + b.Customer.LastName).ToLower().Contains(search)
                        ))
                    )
                );
                ViewBag.Search = search;
            }

            var notifications = await query
                .OrderByDescending(l => l.UpdatedDate)
                .ToListAsync();

            return View(notifications);
        }
        
        // Action: Student xác nhận đã nhận máy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReturnReceived(long bookingId)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên.";
                return RedirectToAction("Login", "Account");
            }

            var booking = await _context.Bookings
                .Include(b => b.Laptop)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.Laptop.StudentId == student.Id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn thuê hoặc bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Notifications));
            }

            // Kiểm tra trạng thái
            if (booking.StatusId != 8)
            {
                TempData["ErrorMessage"] = "Đơn này không ở trạng thái chờ trả máy.";
                return RedirectToAction(nameof(Notifications));
            }

            // Kiểm tra phải có thông báo trước
            if (string.IsNullOrEmpty(booking.RejectReason) || !booking.RejectReason.StartsWith("RETURN_SCHEDULE|"))
            {
                TempData["ErrorMessage"] = "Đơn này chưa có thông báo trả máy.";
                return RedirectToAction(nameof(Notifications));
            }

            try
            {
                // Parse thông tin cũ và thêm flag CONFIRMED
                var parts = booking.RejectReason.Split('|');
                var pickupLocation = parts.Length > 1 ? parts[1] : "Tòa Alpha, L300";
                var appointmentTime = parts.Length > 2 ? parts[2] : DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                
                // Đánh dấu đã xác nhận: RETURN_SCHEDULE|location|time|CONFIRMED|confirmDate
                booking.RejectReason = $"RETURN_SCHEDULE|{pickupLocation}|{appointmentTime}|CONFIRMED|{DateTime.Now:yyyy-MM-dd HH:mm}";
                booking.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã xác nhận nhận máy thành công cho đơn #{bookingId}. Cảm ơn bạn!";
                return RedirectToAction(nameof(Notifications));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xác nhận nhận máy");
                TempData["ErrorMessage"] = "Lỗi hệ thống. Vui lòng thử lại.";
                return RedirectToAction(nameof(Notifications));
            }
        }

        // Action: Đồng ý với giá (xử lý theo StatusId)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptPrice(long id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên.";
                return RedirectToAction("Login", "Account");
            }

            var laptop = await _context.Laptops
                .Include(l => l.Status)
                .Include(l => l.TechnicalTickets) // ✅ THÊM Include để lấy ticket
                .FirstOrDefaultAsync(l => l.Id == id && l.StudentId == student.Id);

            if (laptop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy laptop hoặc bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Rejected));
            }

            var statusName = laptop.Status?.StatusName?.ToLower();

            // Xử lý theo trạng thái
            if (statusName == "fixing")
            {
                // Nếu đang Fixing → Cập nhật Ticket sang StatusId = 4 (Processing)
                laptop.UpdatedDate = DateTime.Now;
                
                // Tìm ticket liên quan (StatusId = 1 và BookingId = null)
                var relatedTicket = laptop.TechnicalTickets
                    .FirstOrDefault(t => t.StatusId == 1 && t.BookingId == null);
                
                if (relatedTicket != null)
                {
                    relatedTicket.StatusId = 4; // Processing - Chuyển sang tab Sửa Chữa
                    relatedTicket.TechnicalResponse += "\n[STUDENT ĐỒNG Ý]: Đã xác nhận cho phép sửa chữa.";
                    relatedTicket.UpdatedDate = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã đồng ý cho sửa chữa! Laptop đang được sửa, vui lòng chờ thông báo.";
                
                // Quay lại trang Rejected để thấy trạng thái cập nhật (nút sẽ biến mất)
                return RedirectToAction(nameof(Rejected));
            }
            else if (statusName == "rejected")
            {
                // Nếu Rejected hoàn toàn → Chuyển sang Approved
                var approvedStatusId = await GetStatusIdAsync("approved");
                if (approvedStatusId == null)
                {
                    TempData["ErrorMessage"] = "Lỗi hệ thống. Vui lòng thử lại sau.";
                    return RedirectToAction(nameof(Rejected));
                }

                laptop.StatusId = approvedStatusId.Value;
                laptop.UpdatedDate = DateTime.Now;
                laptop.RejectReason = null; // Xóa lý do từ chối

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã đồng ý! Laptop của bạn đã được chuyển sang trạng thái Đã phê duyệt.";
                return RedirectToAction(nameof(Approved));
            }
            else
            {
                TempData["ErrorMessage"] = "Laptop này không ở trạng thái hợp lệ.";
                return RedirectToAction(nameof(Rejected));
            }
        }

        // Action: Hủy laptop đang sửa chữa (Fixing) → Chuyển sang Rejected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRejectedLaptop(long id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin sinh viên.";
                return RedirectToAction("Login", "Account");
            }

            var laptop = await _context.Laptops
                .Include(l => l.Status)
                .Include(l => l.TechnicalTickets)
                .FirstOrDefaultAsync(l => l.Id == id && l.StudentId == student.Id);

            if (laptop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy laptop hoặc bạn không có quyền thực hiện thao tác này.";
                return RedirectToAction(nameof(Rejected));
            }

            // Chỉ cho phép hủy khi đang ở trạng thái Fixing
            if (laptop.Status?.StatusName?.ToLower() != "fixing")
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy laptop đang ở trạng thái cần sửa chữa.";
                return RedirectToAction(nameof(Rejected));
            }

            // Lấy StatusId của Rejected
            var rejectedStatusId = await GetStatusIdAsync("rejected");
            if (rejectedStatusId == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToAction(nameof(Rejected));
            }

            // Chuyển laptop sang trạng thái Rejected
            laptop.StatusId = rejectedStatusId.Value;
            laptop.UpdatedDate = DateTime.Now;
            laptop.RejectReason = "Student không đồng ý sửa chữa và đã hủy yêu cầu.";

            // Cập nhật TechnicalTicket sang Rejected (StatusId = 3)
            var relatedTicket = laptop.TechnicalTickets
                .FirstOrDefault(t => t.StatusId == 1 && t.BookingId == null);
            
            if (relatedTicket != null)
            {
                relatedTicket.StatusId = 3; // Rejected
                relatedTicket.TechnicalResponse += "\n[STUDENT HỦY]: Student không đồng ý sửa chữa.";
                relatedTicket.UpdatedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["WarningMessage"] = "Đã hủy yêu cầu sửa chữa. Laptop chuyển sang trạng thái Từ chối.";
            return RedirectToAction(nameof(Rejected));
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