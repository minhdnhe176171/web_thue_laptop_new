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

            // Kiểm tra xem laptop có đang được người khác thuê không (bất kỳ ai)
            var isRentedByOthers = await _context.Bookings
                .AnyAsync(b => b.LaptopId == laptop.Id
                    && (b.StatusId == 2 || b.StatusId == 10)
                    && b.StartTime <= DateTime.Now
                    && b.EndTime >= DateTime.Today);

            if (isRentedByOthers)
            {
                TempData["ErrorMessage"] = "Laptop này hiện đang được thuê bởi người khác. Vui lòng chọn laptop khác hoặc thử lại sau.";
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
                    && b.StatusId == 1);

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
                    && (b.StatusId == 2 || b.StatusId == 10)
                    && b.EndTime >= DateTime.Today)
                .FirstOrDefaultAsync();

            if (activeBooking != null)
            {
                TempData["ErrorMessage"] = $"Bạn đang có một đơn đặt thuê laptop này đang hoạt động (từ {activeBooking.StartTime:dd/MM/yyyy} đến {activeBooking.EndTime:dd/MM/yyyy}). Vui lòng hoàn thành việc trả máy trước khi đặt thuê lại.";
                return RedirectToAction("Details", "Laptop", new { id = id });
            }

            // Tính toán thời gian có thể thuê dựa trên Laptop.EndTime (thời gian student muốn trả máy)
            DateTime? availableStartDate = DateTime.Today;
            DateTime? availableEndDate = null;
            
            if (laptop.EndTime.HasValue)
            {
                // Ngày trả tối đa = EndTime của student - 1 ngày
                availableEndDate = laptop.EndTime.Value.Date.AddDays(-1);
            }

            var model = new BookingViewModel
            {
                LaptopId = laptop.Id,
                Laptop = laptop,
                StartDate = DateTime.Today,
                StartTime = "09:00",
                EndDate = availableEndDate ?? DateTime.Today.AddDays(1),
                PricePerDay = laptop.Price
            };

            ViewBag.AvailableStartDate = availableStartDate;
            ViewBag.AvailableEndDate = availableEndDate;

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

            // Validate ngày nhận - phải >= thời gian thực
            if (model.StartDate < DateTime.Today)
            {
                ModelState.AddModelError("StartDate", "Ngày nhận không được ở quá khứ. Vui lòng chọn từ hôm nay trở đi.");
            }

            // Validate giờ nhận - từ 7h sáng đến 21h tối
            if (!string.IsNullOrEmpty(model.StartTime))
            {
                if (TimeSpan.TryParse(model.StartTime, out TimeSpan time))
                {
                    var hour = time.Hours;
                    if (hour < 7 || hour > 21)
                    {
                        ModelState.AddModelError("StartTime", "Giờ nhận máy phải trong khoảng từ 7h sáng đến 21h tối. Nếu chọn ngoài khoảng thời gian này, đơn hàng sẽ không được duyệt.");
                    }
                }
                else
                {
                    ModelState.AddModelError("StartTime", "Giờ nhận máy không hợp lệ.");
                }
            }

            // Validate ngày trả - phải sau ngày nhận
            if (model.EndDate <= model.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày trả phải sau ngày nhận");
            }

            // Validate ngày trả với thời gian student muốn trả máy
            if (model.Laptop.EndTime.HasValue)
            {
                // Ngày trả tối đa của customer = EndTime của student - 1 ngày
                var maxEndDate = model.Laptop.EndTime.Value.Date.AddDays(-1);
                
                if (model.EndDate >= model.Laptop.EndTime.Value.Date)
                {
                    ModelState.AddModelError("EndDate", $"Ngày trả máy phải nhỏ hơn ngày student muốn nhận lại máy ({model.Laptop.EndTime.Value.Date:dd/MM/yyyy}). Ngày trả tối đa là {maxEndDate:dd/MM/yyyy}.");
                }
            }

            if (!model.AgreeToTerms)
            {
                ModelState.AddModelError("AgreeToTerms", "Bạn phải đồng ý với điều khoản thuê để tiếp tục");
            }

            if (!ModelState.IsValid)
            {
                model.PricePerDay = model.Laptop.Price;
                
                // Tính lại thời gian có thể thuê
                DateTime? availableStartDate = DateTime.Today;
                DateTime? availableEndDate = null;
                if (model.Laptop.EndTime.HasValue)
                {
                    availableEndDate = model.Laptop.EndTime.Value.Date.AddDays(-1);
                }
                ViewBag.AvailableStartDate = availableStartDate;
                ViewBag.AvailableEndDate = availableEndDate;
                
                return View(model);
            }

            // Kiểm tra lại xem laptop có đang được người khác thuê không (double check trước khi tạo booking)
            var isRentedByOthers = await _context.Bookings
                .AnyAsync(b => b.LaptopId == model.LaptopId
                    && (b.StatusId == 2 || b.StatusId == 10)
                    && b.StartTime <= DateTime.Now
                    && b.EndTime >= DateTime.Today);

            if (isRentedByOthers)
            {
                ModelState.AddModelError("", "Laptop này hiện đang được thuê bởi người khác. Vui lòng chọn laptop khác hoặc thử lại sau.");
                model.PricePerDay = model.Laptop.Price;
                
                // Tính lại thời gian có thể thuê
                DateTime? availableStartDate = DateTime.Today;
                DateTime? availableEndDate = null;
                if (model.Laptop.EndTime.HasValue)
                {
                    availableEndDate = model.Laptop.EndTime.Value.Date.AddDays(-1);
                }
                ViewBag.AvailableStartDate = availableStartDate;
                ViewBag.AvailableEndDate = availableEndDate;
                
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
                    && b.StatusId == 1);

            if (pendingBooking != null)
            {
                ModelState.AddModelError("", "Bạn đã có một đơn đặt thuê laptop này đang chờ duyệt. Vui lòng chờ đơn được xử lý trước khi đặt thuê lại.");
                model.PricePerDay = model.Laptop.Price;
                
                // Tính lại thời gian có thể thuê
                DateTime? availableStartDate = DateTime.Today;
                DateTime? availableEndDate = null;
                if (model.Laptop.EndTime.HasValue)
                {
                    availableEndDate = model.Laptop.EndTime.Value.Date.AddDays(-1);
                }
                ViewBag.AvailableStartDate = availableStartDate;
                ViewBag.AvailableEndDate = availableEndDate;
                
                return View(model);
            }

            // Kiểm tra xem customer có booking nào đang active (approved/rented) với laptop này chưa hoàn thành không
            // Chỉ cho phép đặt thuê lại khi booking đã completed/closed
            var activeBooking = await _context.Bookings
                .Include(b => b.Status)
                .Where(b => b.CustomerId == customer.Id 
                    && b.LaptopId == model.LaptopId 
                    && (b.StatusId == 2 || b.StatusId == 10)
                    && b.EndTime >= DateTime.Today)
                .FirstOrDefaultAsync();

            if (activeBooking != null)
            {
                ModelState.AddModelError("", $"Bạn đang có một đơn đặt thuê laptop này đang hoạt động (từ {activeBooking.StartTime:dd/MM/yyyy} đến {activeBooking.EndTime:dd/MM/yyyy}). Vui lòng hoàn thành việc trả máy trước khi đặt thuê lại.");
                model.PricePerDay = model.Laptop.Price;
                
                // Tính lại thời gian có thể thuê
                DateTime? availableStartDate = DateTime.Today;
                DateTime? availableEndDate = null;
                if (model.Laptop.EndTime.HasValue)
                {
                    availableEndDate = model.Laptop.EndTime.Value.Date.AddDays(-1);
                }
                ViewBag.AvailableStartDate = availableStartDate;
                ViewBag.AvailableEndDate = availableEndDate;
                
                return View(model);
            }

            // Lấy StatusId cho booking mới (pending)
            var statusId = await GetStatusIdAsync("pending");
            if (statusId == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
            }

            // Tạo StartTime từ StartDate + StartTime (giờ)
            DateTime startDateTime;
            if (!string.IsNullOrEmpty(model.StartTime) && TimeSpan.TryParse(model.StartTime, out TimeSpan startTimeSpan))
            {
                startDateTime = model.StartDate.Date.Add(startTimeSpan);
            }
            else
            {
                startDateTime = model.StartDate.Date.AddHours(9); // Mặc định 9h sáng
            }

            // Tạo booking
            var booking = new Booking
            {
                CustomerId = customer.Id,
                LaptopId = model.LaptopId,
                StartTime = startDateTime,
                EndTime = model.EndDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59), // Cuối ngày trả
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

        // GET: Booking/MyBookings - Trang theo dõi đơn thuê
        public async Task<IActionResult> MyBookings()
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem đơn thuê của bạn.";
                return RedirectToAction("Login", "Account");
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

            // Lấy tất cả booking của customer này, sắp xếp theo ngày tạo mới nhất
            var allBookings = await _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Status)
                .Where(b => b.CustomerId == customer.Id)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            // Phân loại booking theo trạng thái
            var pendingBookings = allBookings
                .Where(b => b.StatusId == 1)
                .ToList();

            var approvedBookings = allBookings
                .Where(b => b.StatusId == 2)
                .ToList();

            var rentedBookings = allBookings
                .Where(b => b.StatusId == 10)
                .ToList();

            var completedBookings = allBookings
                .Where(b => b.StatusId == 8)
                .ToList();

            var cancelledBookings = allBookings
                .Where(b => b.StatusId == 3)
                .ToList();

            ViewBag.PendingBookings = pendingBookings;
            ViewBag.ApprovedBookings = approvedBookings;
            ViewBag.RentedBookings = rentedBookings;
            ViewBag.CompletedBookings = completedBookings;
            ViewBag.CancelledBookings = cancelledBookings;
            ViewBag.CustomerId = customer.Id;

            return View();
        }

        // GET: Booking/Payment/5 - Trang thanh toán và Phiếu Hẹn Nhận Máy
        public async Task<IActionResult> Payment(long? id)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông tin thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
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

            // Lấy booking với đầy đủ thông tin
            var booking = await _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customer.Id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyBookings");
            }

            // Kiểm tra booking phải ở trạng thái Approved (StatusId = 2)
            if (booking.StatusId != 2)
            {
                if (booking.StatusId == 10)
                {
                    TempData["SuccessMessage"] = "Đơn hàng của bạn đã được xác nhận thanh toán và đang trong quá trình thuê. Cảm ơn quý khách!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Đơn hàng này không ở trạng thái đã duyệt, không thể thanh toán.";
                }
                return RedirectToAction("MyBookings");
            }

            ViewBag.Booking = booking;
            ViewBag.CustomerId = customer.Id;

            return View();
        }

        // GET: Booking/CheckPaymentStatus/5 - Kiểm tra trạng thái thanh toán
        public async Task<IActionResult> CheckPaymentStatus(long? id)
        {
            if (id == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng." });
            }

            // Lấy booking
            var booking = await _context.Bookings
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customer.Id);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            // Kiểm tra nếu đã chuyển sang Rented (StatusId = 10)
            if (booking.StatusId == 10)
            {
                return Json(new 
                { 
                    success = true, 
                    status = "rented",
                    message = "Đã xác nhận giao dịch thành công. Vui lòng trả máy đúng hạn. Cảm ơn quý khách!" 
                });
            }

            // Vẫn còn ở trạng thái Approved
            return Json(new 
            { 
                success = true, 
                status = "approved",
                message = "Đang chờ Staff xác nhận thanh toán..." 
            });
        }


        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }
}

