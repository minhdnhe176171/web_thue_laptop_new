using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Controllers
{
    [Route("student")]
    public class StudentController : Controller
    {
        private readonly Swp391LaptopContext _context;

        public StudentController(Swp391LaptopContext context)
        {
            _context = context;
        }

        // Demo: use a fixed student id. Replace with real authentication.
        private long CurrentStudentId
        {
            get
            {
                var student = _context.Students.FirstOrDefault();
                if (student == null)
                {
                    throw new InvalidOperationException("Không tìm thấy student nào trong database. Vui lòng tạo student trước.");
                }
                return student.Id;
            }
        }

        // Kiểm tra kết nối database
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                // Thử kết nối đến database
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    // Đếm số student trong database
                    var studentCount = await _context.Students.CountAsync();
                    var brandCount = await _context.Brands.CountAsync();
                    var statusCount = await _context.Statuses.CountAsync();
                    
                    return Ok(new
                    {
                        success = true,
                        message = "Kết nối database thành công!",
                        data = new
                        {
                            students = studentCount,
                            brands = brandCount,
                            statuses = statusCount
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không thể kết nối đến database"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi kết nối database",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("home")]
        public async Task<IActionResult> Index(string? search, int page = 1, string? status = null)
        {
            const int pageSize = 10; // Số laptop mỗi trang

            var q = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.LaptopDetails)
                .Where(l => l.StudentId == CurrentStudentId)
                .AsQueryable();

            // Lọc theo search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(l => (l.Name ?? string.Empty).ToLower().Contains(s) || (l.Brand != null && (l.Brand.BrandName ?? string.Empty).ToLower().Contains(s)));
            }

            // Lọc theo status
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusLower = status.ToLower();
                q = q.Where(l => l.Status != null && l.Status.StatusName.ToLower() == statusLower);
            }

            // Tính tổng số records và số trang
            var totalItems = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo page hợp lệ
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var laptops = await q
                .OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Đếm số lượng laptop theo từng trạng thái
            var allLaptops = _context.Laptops
                .Include(l => l.Status)
                .Where(l => l.StudentId == CurrentStudentId);

            ViewBag.TotalAll = await allLaptops.CountAsync();
            ViewBag.TotalPending = await allLaptops.CountAsync(l => l.Status != null && l.Status.StatusName.ToLower() == "pending");
            ViewBag.TotalApproved = await allLaptops.CountAsync(l => l.Status != null && l.Status.StatusName.ToLower() == "approved");
            ViewBag.TotalRejected = await allLaptops.CountAsync(l => l.Status != null && l.Status.StatusName.ToLower() == "rejected");

            ViewBag.Search = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.CurrentStatus = status; // Trạng thái đang được chọn
            
            return View(laptops);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Brands = await _context.Brands.ToListAsync();
            var allowed = new[] { "Đang chờ xử lý", "Đã phê duyệt", "Bị từ chối" };
            ViewBag.Statuses = await _context.Statuses.Where(s => allowed.Contains(s.StatusName)).ToListAsync();
            return View(new Laptop());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Laptop laptop, string? Cpu, string? RamSize, string? RamType, string? Storage, string? Gpu, string? Os, string? ScreenSize, DateTime? EndTime)
        {
            laptop.Name = laptop.Name?.Trim();
            
            // Validate tên máy
            if (string.IsNullOrWhiteSpace(laptop.Name))
            {
                ModelState.AddModelError(nameof(laptop.Name), "Vui lòng nhập tên máy");
            }
            else if (laptop.Name.Length < 3)
            {
                ModelState.AddModelError(nameof(laptop.Name), "Tên máy phải có ít nhất 3 ký tự");
            }
            else if (laptop.Name.Length > 100)
            {
                ModelState.AddModelError(nameof(laptop.Name), "Tên máy không được quá 100 ký tự");
            }
            
            // Validate giá
            if (laptop.Price == null || laptop.Price <= 0)
            {
                ModelState.AddModelError(nameof(laptop.Price), "Vui lòng chọn mức giá thuê");
            }
            else if (laptop.Price < 50000)
            {
                ModelState.AddModelError(nameof(laptop.Price), "Giá thuê tối thiểu 50,000 VNĐ/ngày");
            }
            else if (laptop.Price > 10000000)
            {
                ModelState.AddModelError(nameof(laptop.Price), "Giá thuê tối đa 10,000,000 VNĐ/ngày");
            }

            // Validate hãng
            if (laptop.BrandId == null || laptop.BrandId == 0)
            {
                ModelState.AddModelError(nameof(laptop.BrandId), "Vui lòng chọn hãng laptop");
            }

            // Validate thông số kỹ thuật
            if (string.IsNullOrWhiteSpace(Cpu))
            {
                ModelState.AddModelError("Cpu", "Vui lòng nhập thông tin CPU");
            }
            else if (Cpu.Length < 3)
            {
                ModelState.AddModelError("Cpu", "Thông tin CPU phải có ít nhất 3 ký tự");
            }

            if (string.IsNullOrWhiteSpace(RamSize))
            {
                ModelState.AddModelError("RamSize", "Vui lòng chọn dung lượng RAM");
            }

            if (string.IsNullOrWhiteSpace(Storage))
            {
                ModelState.AddModelError("Storage", "Vui lòng nhập thông tin lưu trữ");
            }
            else if (Storage.Length < 3)
            {
                ModelState.AddModelError("Storage", "Thông tin lưu trữ phải có ít nhất 3 ký tự");
            }

            // Validate thời gian thuê
            if (!EndTime.HasValue)
            {
                ModelState.AddModelError("EndTime", "Vui lòng chọn thời gian đến hạn thuê");
            }
            else if (EndTime.Value <= DateTime.Now)
            {
                ModelState.AddModelError("EndTime", "Thời gian đến hạn phải sau thời điểm hiện tại");
            }
            else if (EndTime.Value <= DateTime.Now.AddHours(1))
            {
                ModelState.AddModelError("EndTime", "Thời gian đến hạn phải sau thời điểm hiện tại ít nhất 1 giờ");
            }
            else if (EndTime.Value > DateTime.Now.AddYears(1))
            {
                ModelState.AddModelError("EndTime", "Thời gian đến hạn không được quá 1 năm");
            }

            // Validate trùng tên
            if (!string.IsNullOrWhiteSpace(laptop.Name) && await _context.Laptops.AnyAsync(l => l.StudentId == CurrentStudentId && l.Name.ToLower() == laptop.Name.ToLower()))
            {
                ModelState.AddModelError(nameof(laptop.Name), "Bạn đã có laptop với tên này. Vui lòng đặt tên khác");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Brands = await _context.Brands.ToListAsync();
                var allowed = new[] { "Đang chờ xử lý", "Đã phê duyệt", "Bị từ chối" };
                ViewBag.Statuses = await _context.Statuses.Where(s => allowed.Contains(s.StatusName)).ToListAsync();
                return View(laptop);
            }

            // Set status mặc định là "Pending" (ID = 1)
            if (laptop.StatusId == null || laptop.StatusId == 0)
            {
                // Tìm status Pending - có thể là ID = 1 hoặc tên "Pending"/"Đang chờ xử lý"
                var pending = await _context.Statuses.FirstOrDefaultAsync(s => 
                    s.Id == 1 || 
                    s.StatusName.ToLower() == "pending" || 
                    s.StatusName.ToLower() == "đang chờ xử lý");
                
                if (pending != null) 
                    laptop.StatusId = pending.Id;
                else
                    laptop.StatusId = 1; // Mặc định ID = 1 nếu không tìm thấy
            }

            laptop.StudentId = CurrentStudentId;
            laptop.CreatedDate = DateTime.Now;
            // Lưu thời gian đến hạn vào UpdatedDate
            laptop.UpdatedDate = EndTime;

            _context.Laptops.Add(laptop);
            await _context.SaveChangesAsync();

            var detail = new LaptopDetail
            {
                LaptopId = laptop.Id,
                Cpu = Cpu?.Trim(),
                RamSize = RamSize,
                RamType = RamType,
                Storage = Storage?.Trim(),
                Gpu = Gpu?.Trim(),
                Os = Os?.Trim(),
                ScreenSize = ScreenSize?.Trim()
            };

            _context.LaptopDetails.Add(detail);
            await _context.SaveChangesAsync();

            // Thông báo thành công
            TempData["SuccessMessage"] = $"Đã tạo laptop '{laptop.Name}' thành công! Vui lòng chờ Staff duyệt.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(long id)
        {
            var laptop = await _context.Laptops
                .Include(l => l.LaptopDetails)
                .FirstOrDefaultAsync(l => l.Id == id && l.StudentId == CurrentStudentId);

            if (laptop == null) return NotFound();

            ViewBag.Brands = await _context.Brands.ToListAsync();
            var allowed = new[] { "Đang chờ xử lý", "Đã phê duyệt", "Bị từ chối" };
            ViewBag.Statuses = await _context.Statuses.Where(s => allowed.Contains(s.StatusName)).ToListAsync();
            ViewBag.LaptopDetail = laptop.LaptopDetails.FirstOrDefault();

            return View(laptop);
        }

        [HttpGet("detail/{id}")]
        public async Task<IActionResult> Detail(long id)
        {
            var laptop = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.LaptopDetails)
                .FirstOrDefaultAsync(l => l.Id == id && l.StudentId == CurrentStudentId);

            if (laptop == null) return NotFound();

            // Lấy thông tin từ CreatedDate (bắt đầu) và UpdatedDate (đến hạn)
            if (laptop.CreatedDate.HasValue && laptop.UpdatedDate.HasValue)
            {
                var timeRemaining = laptop.UpdatedDate.Value - DateTime.Now;
                ViewBag.StartTime = laptop.CreatedDate.Value;
                ViewBag.EndTime = laptop.UpdatedDate.Value;
                ViewBag.TimeRemaining = timeRemaining;
            }
            else
            {
                // Nếu chưa có thời gian đến hạn
                ViewBag.StartTime = null;
                ViewBag.EndTime = null;
                ViewBag.TimeRemaining = null;
            }

            return View(laptop);
        }

        [HttpPost("edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Laptop model, string? Cpu, string? RamSize, string? RamType, string? Storage, string? Gpu, string? Os, string? ScreenSize)
        {
            model.Name = model.Name?.Trim();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Vui lòng nhập tên laptop");
            }

            if (!string.IsNullOrWhiteSpace(model.Name) && await _context.Laptops.AnyAsync(l => l.StudentId == CurrentStudentId && l.Id != model.Id && l.Name.ToLower() == model.Name.ToLower()))
            {
                ModelState.AddModelError(nameof(model.Name), "Đã tồn tại laptop cùng tên");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Brands = await _context.Brands.ToListAsync();
                var allowed = new[] { "Đang chờ xử lý", "Đã phê duyệt", "Bị từ chối" };
                ViewBag.Statuses = await _context.Statuses.Where(s => allowed.Contains(s.StatusName)).ToListAsync();
                return View(model);
            }

            var laptop = await _context.Laptops
                .Include(l => l.LaptopDetails)
                .FirstOrDefaultAsync(l => l.Id == model.Id && l.StudentId == CurrentStudentId);

            if (laptop == null) return NotFound();

            laptop.Name = model.Name;
            laptop.BrandId = model.BrandId;
            laptop.Price = model.Price;
            laptop.StatusId = model.StatusId;
            laptop.UpdatedDate = DateTime.Now;

            var detail = laptop.LaptopDetails.FirstOrDefault();
            if (detail == null)
            {
                detail = new LaptopDetail { LaptopId = laptop.Id };
                _context.LaptopDetails.Add(detail);
            }

            detail.Cpu = Cpu;
            detail.RamSize = RamSize;
            detail.RamType = RamType;
            detail.Storage = Storage;
            detail.Gpu = Gpu;
            detail.Os = Os;
            detail.ScreenSize = ScreenSize;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var laptop = await _context.Laptops.FirstOrDefaultAsync(l => l.Id == id && l.StudentId == CurrentStudentId);
            if (laptop == null) return NotFound();

            _context.Laptops.Remove(laptop);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("report")]
        public async Task<IActionResult> Report(int page = 1)
        {
            const int pageSize = 10;

            var myLaptops = await _context.Laptops.Where(l => l.StudentId == CurrentStudentId).ToListAsync();
            var laptopIds = myLaptops.Select(l => l.Id).ToList();

            var income = await _context.Bookings
                .Where(b => laptopIds.Contains(b.LaptopId))
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;

            var errorsQuery = _context.TechnicalTickets
                .Where(t => laptopIds.Contains(t.LaptopId))
                .OrderByDescending(t => t.CreatedDate);

            // Tính tổng số và phân trang
            var totalItems = await errorsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var errors = await errorsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Income = income;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(errors);
        }
    }
}