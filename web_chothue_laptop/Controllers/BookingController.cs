using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.ViewModels;

namespace web_chothue_laptop.Controllers
{
    public class BookingController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<BookingController> _logger;

        public BookingController(Swp391LaptopContext context, ILogger<BookingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Booking/Create/5
        public async Task<IActionResult> Create(long? id)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đặt thuê laptop.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var laptop = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (laptop == null)
            {
                return NotFound();
            }

            // Kiểm tra laptop có sẵn không
            if (laptop.Status?.StatusName?.ToLower() != "available")
            {
                TempData["ErrorMessage"] = "Laptop này hiện không có sẵn để thuê.";
                return RedirectToAction("Details", "Laptop", new { id = id });
            }

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra xem customer đã có booking nào với laptop này đang pending không
            var pendingBooking = await _context.Bookings
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.CustomerId == customer.Id 
                    && b.LaptopId == laptop.Id 
                    && b.Status.StatusName.ToLower() == "pending");

            if (pendingBooking != null)
            {
                TempData["ErrorMessage"] = "Bạn đã có một đơn đặt thuê laptop này đang chờ duyệt. Vui lòng chờ đơn được xử lý trước khi đặt thuê lại.";
                return RedirectToAction("Details", "Laptop", new { id = id });
            }

            // Kiểm tra xem customer có booking nào đang active (approved) với laptop này không
            var activeBooking = await _context.Bookings
                .Include(b => b.Status)
                .Where(b => b.CustomerId == customer.Id 
                    && b.LaptopId == laptop.Id 
                    && (b.Status.StatusName.ToLower() == "approved" || b.Status.StatusName.ToLower() == "rented")
                    && b.EndTime >= DateTime.Today)
                .FirstOrDefaultAsync();

            if (activeBooking != null)
            {
                TempData["ErrorMessage"] = $"Bạn đang có một đơn đặt thuê laptop này đang hoạt động (từ {activeBooking.StartTime:dd/MM/yyyy} đến {activeBooking.EndTime:dd/MM/yyyy}). Vui lòng hoàn thành việc trả máy trước khi đặt thuê lại.";
                return RedirectToAction("Details", "Laptop", new { id = id });
            }

            var model = new BookingViewModel
            {
                LaptopId = laptop.Id,
                Laptop = laptop,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                PricePerDay = laptop.Price
            };

            return View(model);
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để đặt thuê laptop.";
                return RedirectToAction("Login", "Account");
            }

            // Load lại laptop
            model.Laptop = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .FirstOrDefaultAsync(m => m.Id == model.LaptopId);

            if (model.Laptop == null)
            {
                return NotFound();
            }

            // Validate ngày
            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("StartDate", "Ngày nhận không được ở quá khứ");
            }

            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày trả phải sau ngày nhận");
            }

            if (!model.AgreeToTerms)
            {
                ModelState.AddModelError("AgreeToTerms", "Bạn phải đồng ý với điều khoản thuê để tiếp tục");
            }

            if (!ModelState.IsValid)
            {
                model.PricePerDay = model.Laptop.Price;
                return View(model);
            }

            // Tính số ngày và tổng phí
            var days = (model.EndDate - model.StartDate).Days;
            var totalPrice = model.Laptop.Price.HasValue ? model.Laptop.Price.Value * days : 0;

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra xem customer đã có booking nào với laptop này đang pending không
            var pendingBooking = await _context.Bookings
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.CustomerId == customer.Id 
                    && b.LaptopId == model.LaptopId 
                    && b.Status.StatusName.ToLower() == "pending");

            if (pendingBooking != null)
            {
                ModelState.AddModelError("", "Bạn đã có một đơn đặt thuê laptop này đang chờ duyệt. Vui lòng chờ đơn được xử lý trước khi đặt thuê lại.");
                model.PricePerDay = model.Laptop.Price;
                return View(model);
            }

            // Kiểm tra xem customer có booking nào đang active (approved/rented) với laptop này chưa hoàn thành không
            // Chỉ cho phép đặt thuê lại khi booking đã completed/closed
            var activeBooking = await _context.Bookings
                .Include(b => b.Status)
                .Where(b => b.CustomerId == customer.Id 
                    && b.LaptopId == model.LaptopId 
                    && (b.Status.StatusName.ToLower() == "approved" || b.Status.StatusName.ToLower() == "rented")
                    && b.EndTime >= DateTime.Today)
                .FirstOrDefaultAsync();

            if (activeBooking != null)
            {
                ModelState.AddModelError("", $"Bạn đang có một đơn đặt thuê laptop này đang hoạt động (từ {activeBooking.StartTime:dd/MM/yyyy} đến {activeBooking.EndTime:dd/MM/yyyy}). Vui lòng hoàn thành việc trả máy trước khi đặt thuê lại.");
                model.PricePerDay = model.Laptop.Price;
                return View(model);
            }

            // Lấy StatusId cho booking mới (pending)
            var statusId = await GetStatusIdAsync("pending");
            if (statusId == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
            }

            // Tạo booking
            var booking = new Booking
            {
                CustomerId = customer.Id,
                LaptopId = model.LaptopId,
                StartTime = model.StartDate,
                EndTime = model.EndDate,
                TotalPrice = totalPrice,
                StatusId = statusId.Value,
                CreatedDate = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt thuê thành công! Chúng tôi sẽ xử lý yêu cầu của bạn sớm nhất.";
            return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
        }

        // Tính phí thuê (AJAX)
        [HttpPost]
        public async Task<IActionResult> CalculatePrice(long laptopId, DateTime startDate, DateTime endDate)
        {
            var laptop = await _context.Laptops.FindAsync(laptopId);
            if (laptop == null || !laptop.Price.HasValue)
            {
                return Json(new { success = false, message = "Không tìm thấy laptop hoặc chưa có giá" });
            }

            if (endDate <= startDate)
            {
                return Json(new { success = false, message = "Ngày trả phải sau ngày nhận" });
            }

            var days = (endDate - startDate).Days;
            var totalPrice = laptop.Price.Value * days;

            return Json(new 
            { 
                success = true, 
                days = days, 
                pricePerDay = laptop.Price.Value, 
                totalPrice = totalPrice 
            });
        }

        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }
}

