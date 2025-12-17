using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.ViewModels;
using web_chothue_laptop.Services;
using System.Net;

namespace web_chothue_laptop.Controllers
{
    public class BookingController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<BookingController> _logger;
        private readonly VnpayService _vnpayService;
        private readonly IConfiguration _configuration;

        public BookingController(Swp391LaptopContext context, ILogger<BookingController> logger, VnpayService vnpayService, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _vnpayService = vnpayService;
            _configuration = configuration;
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
        public async Task<IActionResult> MyBookings(string? tab, string? search, int? status, int? page)
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

            const int PageSize = 10;
            int pageIndex = page ?? 1;
            tab = tab ?? "all";

            // Base query: Lấy tất cả booking của customer này
            IQueryable<Booking> baseQuery = _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Status)
                .Where(b => b.CustomerId == customer.Id);

            // Filter theo search
            if (!string.IsNullOrWhiteSpace(search))
            {
                baseQuery = baseQuery.Where(b => 
                    b.Id.ToString().Contains(search) ||
                    (b.Laptop != null && b.Laptop.Name.Contains(search)) ||
                    (b.Laptop != null && b.Laptop.Brand != null && b.Laptop.Brand.BrandName.Contains(search)));
            }

            // Filter theo status
            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(b => b.StatusId == status.Value);
            }

            // Tạo query cho từng tab (thêm OrderByDescending cho mỗi query)
            var allQuery = baseQuery.OrderByDescending(b => b.CreatedDate);
            var pendingQuery = baseQuery.Where(b => b.StatusId == 1).OrderByDescending(b => b.CreatedDate);
            var approvedQuery = baseQuery.Where(b => b.StatusId == 2).OrderByDescending(b => b.CreatedDate);
            var rentedQuery = baseQuery.Where(b => b.StatusId == 10).OrderByDescending(b => b.CreatedDate);
            var completedQuery = baseQuery.Where(b => b.StatusId == 8).OrderByDescending(b => b.CreatedDate);
            var cancelledQuery = baseQuery.Where(b => b.StatusId == 3).OrderByDescending(b => b.CreatedDate);

            // Tạo PaginatedList cho tab hiện tại
            PaginatedList<Booking>? paginatedBookings = null;
            PaginatedList<Booking>? paginatedPending = null;
            PaginatedList<Booking>? paginatedApproved = null;
            PaginatedList<Booking>? paginatedRented = null;
            PaginatedList<Booking>? paginatedCompleted = null;
            PaginatedList<Booking>? paginatedCancelled = null;

            switch (tab.ToLower())
            {
                case "pending":
                    paginatedPending = await PaginatedList<Booking>.CreateAsync(pendingQuery, pageIndex, PageSize);
                    break;
                case "approved":
                    paginatedApproved = await PaginatedList<Booking>.CreateAsync(approvedQuery, pageIndex, PageSize);
                    break;
                case "rented":
                    paginatedRented = await PaginatedList<Booking>.CreateAsync(rentedQuery, pageIndex, PageSize);
                    break;
                case "completed":
                    paginatedCompleted = await PaginatedList<Booking>.CreateAsync(completedQuery, pageIndex, PageSize);
                    break;
                case "cancelled":
                    paginatedCancelled = await PaginatedList<Booking>.CreateAsync(cancelledQuery, pageIndex, PageSize);
                    break;
                default: // "all"
                    paginatedBookings = await PaginatedList<Booking>.CreateAsync(allQuery, pageIndex, PageSize);
                    break;
            }

            // Lấy số lượng cho mỗi tab (không phân trang)
            var pendingCount = await pendingQuery.CountAsync();
            var approvedCount = await approvedQuery.CountAsync();
            var rentedCount = await rentedQuery.CountAsync();
            var completedCount = await completedQuery.CountAsync();
            var cancelledCount = await cancelledQuery.CountAsync();

            // Set ViewBag
            ViewBag.Tab = tab;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.PageIndex = pageIndex;
            ViewBag.PaginatedBookings = paginatedBookings;
            ViewBag.PaginatedPending = paginatedPending;
            ViewBag.PaginatedApproved = paginatedApproved;
            ViewBag.PaginatedRented = paginatedRented;
            ViewBag.PaginatedCompleted = paginatedCompleted;
            ViewBag.PaginatedCancelled = paginatedCancelled;
            ViewBag.PendingCount = pendingCount;
            ViewBag.ApprovedCount = approvedCount;
            ViewBag.RentedCount = rentedCount;
            ViewBag.CompletedCount = completedCount;
            ViewBag.CancelledCount = cancelledCount;
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

            // Kiểm tra nếu đã thanh toán (StatusId = 12 - Banked)
            if (booking.StatusId == 12)
            {
                return Json(new 
                { 
                    success = true, 
                    status = "paid",
                    message = "Thanh toán thành công! Vui lòng đến gặp Staff để nhận máy." 
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

        // GET: Booking/OnlinePayment
        [HttpGet]
        public async Task<IActionResult> OnlinePayment(long? bookingId)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thanh toán online.";
                return RedirectToAction("Login", "Account");
            }

            // Nếu có bookingId, load thông tin booking
            if (bookingId.HasValue)
            {
                var userIdLong = long.Parse(userId);
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                    return View();
                }

                var booking = await _context.Bookings
                    .Include(b => b.Laptop)
                        .ThenInclude(l => l.Brand)
                    .Include(b => b.Status)
                    .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id);

                if (booking != null)
                {
                    // Cho phép hiển thị khi đã duyệt (StatusId = 2) hoặc đã thanh toán (StatusId = 12)
                    if (booking.StatusId == 2 || booking.StatusId == 12)
                    {
                        // Tạo VietQR code cho MBBank (chuyển khoản trực tiếp)
                        var qrCodeUrl = _vnpayService.CreateVietQrCode(booking.TotalPrice ?? 0, booking.Id);
                        
                        // Lấy thông tin tài khoản từ config
                        var accountName = _configuration["BankTransfer:AccountName"] ?? "Ha Hoang Hiep";
                        var accountNumber = _configuration["BankTransfer:AccountNumber"] ?? "0862735289";
                        var bankCode = _configuration["BankTransfer:BankCode"] ?? "MB";
                        var phoneNumber = _configuration["BankTransfer:PhoneNumber"] ?? "0862735289";

                        ViewBag.Booking = booking;
                        ViewBag.QrCodeUrl = qrCodeUrl;
                        ViewBag.AccountName = accountName;
                        ViewBag.AccountNumber = accountNumber;
                        ViewBag.BankName = "MBBank";
                        ViewBag.PhoneNumber = phoneNumber;
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Đơn hàng này không ở trạng thái đã duyệt, không thể thanh toán.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                }
            }

            return View();
        }

        // POST: Booking/OnlinePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnlinePaymentPost([FromForm] long? bookingId)
        {
            if (!bookingId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã đơn hàng.";
                return RedirectToAction("OnlinePayment");
            }

            return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
        }

        // GET: Booking/VnpayReturn - Callback từ VNPay sau khi thanh toán
        public async Task<IActionResult> VnpayReturn()
        {
            _logger.LogInformation("VNPay Return - Begin, URL: {RawUrl}", Request.QueryString);
            
            if (Request.Query.Count > 0)
            {
                var vnp_Params = new Dictionary<string, string>();
                foreach (var key in Request.Query.Keys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnp_Params.Add(key, Request.Query[key].ToString());
                    }
                }

                var vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();
                var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
                var vnp_TransactionStatus = Request.Query["vnp_TransactionStatus"].ToString();
                var vnp_TxnRef = Request.Query["vnp_TxnRef"].ToString();
                var vnp_Amount = Request.Query["vnp_Amount"].ToString();
                var vnp_TransactionNo = Request.Query["vnp_TransactionNo"].ToString();

                _logger.LogInformation("VNPay Return - ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}, TxnRef: {TxnRef}, Amount: {Amount}", 
                    vnp_ResponseCode, vnp_TransactionStatus, vnp_TxnRef, vnp_Amount);

                // Validate signature
                if (!_vnpayService.ValidateSignature(vnp_Params, vnp_SecureHash))
                {
                    _logger.LogWarning("VNPay Return - Invalid signature, InputData: {RawUrl}", Request.QueryString);
                    TempData["ErrorMessage"] = "Chữ ký không hợp lệ. Vui lòng liên hệ hỗ trợ.";
                    if (long.TryParse(vnp_TxnRef, out long bookingId))
                    {
                        return RedirectToAction("OnlinePayment", new { bookingId = bookingId });
                    }
                    return RedirectToAction("MyBookings");
                }

                // Kiểm tra response code và transaction status (theo chuẩn VNPay)
                if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                {
                    // Thanh toán thành công
                    if (long.TryParse(vnp_TxnRef, out long bookingId))
                    {
                        _logger.LogInformation("VNPay Return - Processing payment for booking {BookingId}, VNPay TranId: {TranId}", 
                            bookingId, vnp_TransactionNo);
                        
                        var booking = await _context.Bookings
                            .Include(b => b.Status)
                            .FirstOrDefaultAsync(b => b.Id == bookingId);

                        if (booking != null)
                        {
                            // Kiểm tra số tiền
                            long vnp_AmountLong = long.Parse(vnp_Amount) / 100; // VNPay trả về số tiền nhân 100
                            long bookingAmount = (long)(booking.TotalPrice ?? 0);
                            
                            if (bookingAmount == vnp_AmountLong)
                            {
                                _logger.LogInformation("VNPay Return - Found booking {BookingId}, Current StatusId: {StatusId}, Amount match: {Amount}", 
                                    bookingId, booking.StatusId, vnp_AmountLong);
                                
                                // Cập nhật StatusId = 12 (Banked) khi thanh toán thành công
                                if (booking.StatusId == 1 || booking.StatusId == 2) // Pending hoặc Approved
                                {
                                    var bankedStatusId = await GetStatusIdAsync("banked");
                                    if (bankedStatusId.HasValue)
                                    {
                                        booking.StatusId = bankedStatusId.Value;
                                        booking.UpdatedDate = DateTime.Now;
                                        
                                        try
                                        {
                                            await _context.SaveChangesAsync();
                                            
                                            _logger.LogInformation("VNPay Return - Successfully updated booking {BookingId} to StatusId: {StatusId}, VNPay TranId: {TranId}", 
                                                bookingId, booking.StatusId, vnp_TransactionNo);
                                            
                                            TempData["SuccessMessage"] = "Thanh toán thành công! Vui lòng đến gặp Staff để nhận máy.";
                                            return RedirectToAction("OnlinePayment", new { bookingId = bookingId });
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "VNPay Return - Error saving booking {BookingId}", bookingId);
                                            TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái thanh toán. Vui lòng liên hệ hỗ trợ.";
                                            return RedirectToAction("OnlinePayment", new { bookingId = bookingId });
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogInformation("VNPay Return - Booking {BookingId} already processed, StatusId: {StatusId}", 
                                        bookingId, booking.StatusId);
                                    TempData["SuccessMessage"] = "Đơn hàng đã được thanh toán thành công.";
                                    return RedirectToAction("OnlinePayment", new { bookingId = bookingId });
                                }
                            }
                            else
                            {
                                _logger.LogWarning("VNPay Return - Amount mismatch. Booking: {BookingAmount}, VNPay: {VnpAmount}", 
                                    bookingAmount, vnp_AmountLong);
                                TempData["ErrorMessage"] = "Số tiền thanh toán không khớp. Vui lòng liên hệ hỗ trợ.";
                                return RedirectToAction("OnlinePayment", new { bookingId = bookingId });
                            }
                        }
                        else
                        {
                            _logger.LogWarning("VNPay Return - Booking {BookingId} not found", bookingId);
                            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("VNPay Return - Payment failed. ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}", 
                        vnp_ResponseCode, vnp_TransactionStatus);
                    TempData["ErrorMessage"] = $"Thanh toán không thành công. Mã lỗi: {vnp_ResponseCode}";
                    if (long.TryParse(vnp_TxnRef, out long bookingId))
                    {
                        return RedirectToAction("OnlinePayment", new { bookingId = bookingId });
                    }
                }
            }

            return RedirectToAction("MyBookings");
        }

        // POST: Booking/VnpayIpn - IPN (Instant Payment Notification) từ VNPay
        [HttpPost]
        public async Task<IActionResult> VnpayIpn()
        {
            string returnContent = string.Empty;
            
            _logger.LogInformation("VNPay IPN - Begin, Form count: {Count}", Request.Form.Count);
            
            if (Request.Form.Count > 0)
            {
                var vnp_Params = new Dictionary<string, string>();
                foreach (var key in Request.Form.Keys)
                {
                    if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                    {
                        vnp_Params.Add(key, Request.Form[key].ToString());
                    }
                }

                var vnp_SecureHash = Request.Form["vnp_SecureHash"].ToString();
                var vnp_ResponseCode = Request.Form["vnp_ResponseCode"].ToString();
                var vnp_TransactionStatus = Request.Form["vnp_TransactionStatus"].ToString();
                var vnp_TxnRef = Request.Form["vnp_TxnRef"].ToString();
                var vnp_Amount = Request.Form["vnp_Amount"].ToString();
                var vnp_TransactionNo = Request.Form["vnp_TransactionNo"].ToString();

                _logger.LogInformation("VNPay IPN - ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}, TxnRef: {TxnRef}, Amount: {Amount}", 
                    vnp_ResponseCode, vnp_TransactionStatus, vnp_TxnRef, vnp_Amount);

                // Validate signature
                if (!_vnpayService.ValidateSignature(vnp_Params, vnp_SecureHash))
                {
                    _logger.LogWarning("VNPay IPN - Invalid signature, InputData: {RawUrl}", Request.QueryString);
                    returnContent = "{\"RspCode\":\"97\",\"Message\":\"Invalid signature\"}";
                }
                else
                {
                    // Kiểm tra response code và transaction status (theo chuẩn VNPay)
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        // Thanh toán thành công
                        if (long.TryParse(vnp_TxnRef, out long bookingId))
                        {
                            _logger.LogInformation("VNPay IPN - Processing payment for booking {BookingId}, VNPay TranId: {TranId}", 
                                bookingId, vnp_TransactionNo);
                            
                            var booking = await _context.Bookings
                                .Include(b => b.Status)
                                .FirstOrDefaultAsync(b => b.Id == bookingId);

                            if (booking != null)
                            {
                                // Kiểm tra số tiền
                                long vnp_AmountLong = long.Parse(vnp_Amount) / 100; // VNPay trả về số tiền nhân 100
                                long bookingAmount = (long)(booking.TotalPrice ?? 0);
                                
                                if (bookingAmount == vnp_AmountLong)
                                {
                                    // Kiểm tra trạng thái order
                                    if (booking.StatusId == 1 || booking.StatusId == 2) // Pending hoặc Approved
                                    {
                                        var bankedStatusId = await GetStatusIdAsync("banked");
                                        if (bankedStatusId.HasValue)
                                        {
                                            booking.StatusId = bankedStatusId.Value;
                                            booking.UpdatedDate = DateTime.Now;
                                            
                                            try
                                            {
                                                await _context.SaveChangesAsync();
                                                _logger.LogInformation("VNPay IPN - Successfully updated booking {BookingId} to StatusId: {StatusId}, VNPay TranId: {TranId}", 
                                                    bookingId, booking.StatusId, vnp_TransactionNo);
                                                returnContent = "{\"RspCode\":\"00\",\"Message\":\"Confirm Success\"}";
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, "VNPay IPN - Error saving booking {BookingId}", bookingId);
                                                returnContent = "{\"RspCode\":\"99\",\"Message\":\"Database error\"}";
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogWarning("VNPay IPN - Banked status not found");
                                            returnContent = "{\"RspCode\":\"99\",\"Message\":\"Status not found\"}";
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogInformation("VNPay IPN - Booking {BookingId} already confirmed, StatusId: {StatusId}", 
                                            bookingId, booking.StatusId);
                                        returnContent = "{\"RspCode\":\"02\",\"Message\":\"Order already confirmed\"}";
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("VNPay IPN - Amount mismatch. Booking: {BookingAmount}, VNPay: {VnpAmount}", 
                                        bookingAmount, vnp_AmountLong);
                                    returnContent = "{\"RspCode\":\"04\",\"Message\":\"invalid amount\"}";
                                }
                            }
                            else
                            {
                                _logger.LogWarning("VNPay IPN - Booking {BookingId} not found", bookingId);
                                returnContent = "{\"RspCode\":\"01\",\"Message\":\"Order not found\"}";
                            }
                        }
                        else
                        {
                            returnContent = "{\"RspCode\":\"01\",\"Message\":\"Invalid order ID\"}";
                        }
                    }
                    else
                    {
                        _logger.LogWarning("VNPay IPN - Payment failed. ResponseCode: {ResponseCode}, TransactionStatus: {TransactionStatus}", 
                            vnp_ResponseCode, vnp_TransactionStatus);
                        returnContent = $"{{\"RspCode\":\"{vnp_ResponseCode}\",\"Message\":\"Payment failed\"}}";
                    }
                }
            }
            else
            {
                returnContent = "{\"RspCode\":\"99\",\"Message\":\"Input data required\"}";
            }

            Response.ContentType = "application/json";
            return Content(returnContent);
        }

        // POST: Booking/BankTransferWebhook - Webhook để nhận thông báo chuyển khoản từ Sepay
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Cho phép webhook từ bên ngoài
        [Route("")]
        public async Task<IActionResult> BankTransferWebhook([FromBody] SepayWebhookRequest request)
        {
            try
            {
                _logger.LogInformation("BankTransferWebhook - Received Sepay webhook: Content: {Content}, Amount: {Amount}, TransactionId: {TransactionId}, ReferenceCode: {ReferenceCode}", 
                    request?.Content, request?.TransferAmount, request?.Id, request?.ReferenceCode);

                if (request == null)
                {
                    _logger.LogWarning("BankTransferWebhook - Request is null");
                    return BadRequest(new { success = false, message = "Invalid request data" });
                }

                // Chỉ xử lý giao dịch tiền vào (transferType = "in")
                if (request.TransferType != "in")
                {
                    _logger.LogInformation("BankTransferWebhook - Ignoring non-incoming transaction. TransferType: {TransferType}", request.TransferType);
                    return Ok(new { success = true, message = "Ignored non-incoming transaction" });
                }

                // Parse bookingId từ content: "THUE LAPTOP 23 Ma giao dich..." -> bookingId = 23
                long? bookingId = null;
                if (!string.IsNullOrEmpty(request.Content))
                {
                    // Tìm pattern "THUE LAPTOP {number}" hoặc "THUE LAPTOP #{number}"
                    var match = System.Text.RegularExpressions.Regex.Match(request.Content, @"THUE\s+LAPTOP\s+#?(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        if (long.TryParse(match.Groups[1].Value, out long parsedId))
                        {
                            bookingId = parsedId;
                        }
                    }
                }

                if (!bookingId.HasValue)
                {
                    _logger.LogWarning("BankTransferWebhook - Could not extract bookingId from content: {Content}", request.Content);
                    return BadRequest(new { success = false, message = "Could not extract booking ID from content" });
                }

                _logger.LogInformation("BankTransferWebhook - Extracted bookingId: {BookingId} from content: {Content}", bookingId.Value, request.Content);

                var booking = await _context.Bookings
                    .Include(b => b.Status)
                    .FirstOrDefaultAsync(b => b.Id == bookingId.Value);

                if (booking == null)
                {
                    _logger.LogWarning("BankTransferWebhook - Booking {BookingId} not found", bookingId.Value);
                    return NotFound(new { success = false, message = $"Booking {bookingId.Value} not found" });
                }

                // Kiểm tra số tiền
                if (request.TransferAmount.HasValue)
                {
                    var bookingAmount = (long)(booking.TotalPrice ?? 0);
                    var receivedAmount = request.TransferAmount.Value;
                    
                    // Cho phép sai số nhỏ (có thể do làm tròn)
                    if (Math.Abs(bookingAmount - receivedAmount) > 1000) // Cho phép sai số 1000 VND
                    {
                        _logger.LogWarning("BankTransferWebhook - Amount mismatch. Booking: {BookingAmount}, Received: {ReceivedAmount}", 
                            bookingAmount, receivedAmount);
                        return BadRequest(new { success = false, message = $"Amount mismatch. Expected: {bookingAmount}, Received: {receivedAmount}" });
                    }
                }

                // Cập nhật trạng thái nếu chưa thanh toán
                if (booking.StatusId == 1 || booking.StatusId == 2) // Pending hoặc Approved
                {
                    var bankedStatusId = await GetStatusIdAsync("banked");
                    if (bankedStatusId.HasValue)
                    {
                        booking.StatusId = bankedStatusId.Value;
                        booking.UpdatedDate = DateTime.Now;
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("BankTransferWebhook - Successfully updated booking {BookingId} to StatusId: {StatusId}, Sepay TransactionId: {TransactionId}, ReferenceCode: {ReferenceCode}", 
                            booking.Id, booking.StatusId, request.Id, request.ReferenceCode);
                        
                        return Ok(new { success = true, message = "Payment confirmed", bookingId = booking.Id, transactionId = request.Id });
                    }
                    else
                    {
                        _logger.LogError("BankTransferWebhook - Banked status not found in database");
                        return StatusCode(500, new { success = false, message = "Banked status not found" });
                    }
                }
                else
                {
                    _logger.LogInformation("BankTransferWebhook - Booking {BookingId} already processed, StatusId: {StatusId}", 
                        booking.Id, booking.StatusId);
                    return Ok(new { success = true, message = "Already confirmed", bookingId = booking.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BankTransferWebhook - Error processing webhook. Request: {Request}", 
                    System.Text.Json.JsonSerializer.Serialize(request));
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        // POST: Booking/ConfirmPayment - Xác nhận thanh toán (khi dùng VietQR không có callback)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment([FromForm] long? bookingId)
        {
            if (!bookingId.HasValue)
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
                .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            // Chỉ cho phép xác nhận khi StatusId = 2 (Approved)
            if (booking.StatusId == 2)
            {
                var bankedStatusId = await GetStatusIdAsync("banked");
                if (bankedStatusId.HasValue)
                {
                    booking.StatusId = bankedStatusId.Value;
                    booking.UpdatedDate = DateTime.Now;
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("ConfirmPayment - Successfully updated booking {BookingId} to StatusId: {StatusId}", 
                            bookingId.Value, booking.StatusId);
                        
                        TempData["SuccessMessage"] = "Thanh toán thành công! Vui lòng đến gặp Staff để nhận máy.";
                        return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ConfirmPayment - Error saving booking {BookingId}", bookingId.Value);
                        TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái thanh toán. Vui lòng thử lại.";
                        return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy trạng thái 'banked' trong hệ thống.";
                    return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
                }
            }
            else if (booking.StatusId == 12)
            {
                TempData["SuccessMessage"] = "Đơn hàng đã được thanh toán thành công.";
                return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
            }
            else
            {
                TempData["ErrorMessage"] = "Đơn hàng này không ở trạng thái đã duyệt, không thể xác nhận thanh toán.";
                return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
            }
        }

        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }

    // Model cho Sepay Webhook Request
    public class SepayWebhookRequest
    {
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? SubAccount { get; set; }
        public string? Code { get; set; }
        public string? Content { get; set; } // "THUE LAPTOP 23 Ma giao dich..."
        public string? TransferType { get; set; } // "in" hoặc "out"
        public string? Description { get; set; }
        public long? TransferAmount { get; set; } // Số tiền (VND)
        public string? ReferenceCode { get; set; }
        public long? Accumulated { get; set; }
        public long? Id { get; set; } // Transaction ID từ Sepay
    }
}

