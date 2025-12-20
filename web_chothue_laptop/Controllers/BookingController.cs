using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.ViewModels;
using web_chothue_laptop.Services;
using System.Net;
using System.IO;
using Net.payOS.Types;

namespace web_chothue_laptop.Controllers
{
    public class BookingController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<BookingController> _logger;
        private readonly VnpayService _vnpayService;
        private readonly PayOSService _payOSService;
        private readonly IConfiguration _configuration;
        private readonly CloudinaryService _cloudinaryService;

        public BookingController(Swp391LaptopContext context, ILogger<BookingController> logger, VnpayService vnpayService, PayOSService payOSService, IConfiguration configuration, CloudinaryService cloudinaryService)
        {
            _context = context;
            _logger = logger;
            _vnpayService = vnpayService;
            _payOSService = payOSService;
            _configuration = configuration;
            _cloudinaryService = cloudinaryService;
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

            // Kiểm tra xem laptop có đang được người khác thuê không (bất kỳ ai) - bao gồm cả đã chuyển khoản (banked)
            var isRentedByOthers = await _context.Bookings
                .AnyAsync(b => b.LaptopId == laptop.Id
                    && (b.StatusId == 2 || b.StatusId == 10 || b.StatusId == 12) // approved, rented, banked (đã chuyển khoản)
                    && b.EndTime >= DateTime.Today);

            if (isRentedByOthers)
            {
                TempData["ErrorMessage"] = "Laptop này đã có người chuyển tiền và chờ lấy máy. Vui lòng đặt máy khác.";
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

            // VALIDATE: Mỗi customer chỉ được thuê 1 máy tại một thời điểm (bất kỳ laptop nào)
            // Kiểm tra xem customer đã có booking nào đang active (đã thanh toán thành công hoặc đang thuê) với bất kỳ laptop nào không
            var activeBookingAnyLaptop = await _context.Bookings
                .Include(b => b.Status)
                .Include(b => b.Laptop)
                .Where(b => b.CustomerId == customer.Id
                    && (b.StatusId == 12 // banked (đã thanh toán thành công)
                        || b.StatusId == 10 // rented (đang thuê)
                        || (b.StatusId == 2 && b.EndTime >= DateTime.Today)) // approved và chưa hết hạn
                    && b.EndTime >= DateTime.Today)
                .FirstOrDefaultAsync();

            if (activeBookingAnyLaptop != null)
            {
                TempData["ErrorMessage"] = $"Bạn đang có một đơn thuê laptop đang hoạt động (từ {activeBookingAnyLaptop.StartTime:dd/MM/yyyy} đến {activeBookingAnyLaptop.EndTime:dd/MM/yyyy}). Vui lòng hoàn thành việc trả máy trước khi đặt thuê laptop mới.";
                return RedirectToAction("MyBookings");
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

            // Kiểm tra lại xem laptop có đang được người khác thuê không (double check trước khi tạo booking) - bao gồm cả đã chuyển khoản (banked)
            var isRentedByOthers = await _context.Bookings
                .AnyAsync(b => b.LaptopId == model.LaptopId
                    && (b.StatusId == 2 || b.StatusId == 10 || b.StatusId == 12) // approved, rented, banked (đã chuyển khoản)
                    && b.EndTime >= DateTime.Today);

            if (isRentedByOthers)
            {
                ModelState.AddModelError("", "Laptop này đã có người chuyển tiền và chờ lấy máy. Vui lòng đặt máy khác.");
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

            // VALIDATE: Mỗi customer chỉ được thuê 1 máy tại một thời điểm (bất kỳ laptop nào)
            // Kiểm tra xem customer đã có booking nào đang active (đã thanh toán thành công hoặc đang thuê) với bất kỳ laptop nào không
            var activeBookingAnyLaptop = await _context.Bookings
                .Include(b => b.Status)
                .Include(b => b.Laptop)
                .Where(b => b.CustomerId == customer.Id
                    && (b.StatusId == 12 // banked (đã thanh toán thành công)
                        || b.StatusId == 10 // rented (đang thuê)
                        || (b.StatusId == 2 && b.EndTime >= DateTime.Today)) // approved và chưa hết hạn
                    && b.EndTime >= DateTime.Today)
                .FirstOrDefaultAsync();

            if (activeBookingAnyLaptop != null)
            {
                ModelState.AddModelError("", $"Bạn đang có một đơn thuê laptop đang hoạt động (từ {activeBookingAnyLaptop.StartTime:dd/MM/yyyy} đến {activeBookingAnyLaptop.EndTime:dd/MM/yyyy}). Vui lòng hoàn thành việc trả máy trước khi đặt thuê laptop mới.");
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

            // Kiểm tra đã upload ảnh CCCD và thẻ sinh viên chưa
            if (string.IsNullOrEmpty(booking.IdNoUrl) || string.IsNullOrEmpty(booking.StudentUrl))
            {
                TempData["ErrorMessage"] = "Vui lòng upload ảnh CCCD và thẻ sinh viên trước khi thanh toán.";
                return RedirectToAction("UploadDocuments", new { id = booking.Id });
            }

            // Kiểm tra thời gian hợp lệ - chỉ cho phép thanh toán khi đơn đang trong thời gian hợp lệ
            var now = DateTime.Now;
            if (booking.EndTime < now)
            {
                TempData["ErrorMessage"] = $"Đơn hàng này đã hết thời hạn thanh toán. Thời gian thuê đã kết thúc vào {booking.EndTime:dd/MM/yyyy HH:mm}.";
                return RedirectToAction("MyBookings");
            }
            if (booking.StartTime > now)
            {
                TempData["ErrorMessage"] = $"Đơn hàng này chưa đến thời gian thanh toán. Thời gian thuê bắt đầu từ {booking.StartTime:dd/MM/yyyy HH:mm}.";
                return RedirectToAction("MyBookings");
            }

            ViewBag.Booking = booking;
            ViewBag.CustomerId = customer.Id;

            return View();
        }

        // GET: Booking/UploadDocuments/5 - Trang upload ảnh CCCD và thẻ sinh viên
        public async Task<IActionResult> UploadDocuments(long? id)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để upload tài liệu.";
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
                TempData["ErrorMessage"] = "Đơn hàng này không ở trạng thái đã duyệt, không thể upload tài liệu.";
                return RedirectToAction("MyBookings");
            }

            ViewBag.Booking = booking;
            ViewBag.CustomerId = customer.Id;

            return View();
        }

        // POST: Booking/UploadDocuments/5 - Xử lý upload ảnh CCCD và thẻ sinh viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocuments(long? id, IFormFile? idNoFile, IFormFile? studentCardFile)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để upload tài liệu.";
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

            // Lấy booking
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
                TempData["ErrorMessage"] = "Đơn hàng này không ở trạng thái đã duyệt, không thể upload tài liệu.";
                return RedirectToAction("MyBookings");
            }

            // Kiểm tra đã có file upload
            bool hasNewUpload = false;

            try
            {
                // Upload ảnh CCCD nếu có
                if (idNoFile != null && idNoFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(idNoFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "File ảnh CCCD không đúng định dạng. Chỉ chấp nhận: .jpg, .jpeg, .png, .gif, .webp";
                        ViewBag.Booking = booking;
                        ViewBag.CustomerId = customer.Id;
                        return View();
                    }

                    var idNoUrl = await _cloudinaryService.UploadImageAsync(idNoFile, "booking-documents");
                    booking.IdNoUrl = idNoUrl;
                    hasNewUpload = true;
                }

                // Upload ảnh thẻ sinh viên nếu có
                if (studentCardFile != null && studentCardFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    var fileExtension = Path.GetExtension(studentCardFile.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "File ảnh thẻ sinh viên không đúng định dạng. Chỉ chấp nhận: .jpg, .jpeg, .png, .gif, .webp";
                        ViewBag.Booking = booking;
                        ViewBag.CustomerId = customer.Id;
                        return View();
                    }

                    var studentUrl = await _cloudinaryService.UploadImageAsync(studentCardFile, "booking-documents");
                    booking.StudentUrl = studentUrl;
                    hasNewUpload = true;
                }

                // Kiểm tra bắt buộc phải có cả 2 ảnh
                if (string.IsNullOrEmpty(booking.IdNoUrl) || string.IsNullOrEmpty(booking.StudentUrl))
                {
                    if (hasNewUpload)
                    {
                        await _context.SaveChangesAsync();
                        TempData["WarningMessage"] = "Vui lòng upload đầy đủ cả ảnh CCCD và thẻ sinh viên trước khi tiếp tục.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Vui lòng chọn file để upload.";
                    }
                    ViewBag.Booking = booking;
                    ViewBag.CustomerId = customer.Id;
                    return View();
                }

                // Lưu thay đổi
                booking.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Upload ảnh thành công! Bạn có thể tiến hành thanh toán ngay bây giờ.";
                return RedirectToAction("Payment", new { id = booking.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading documents for booking {BookingId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi upload ảnh. Vui lòng thử lại.";
                ViewBag.Booking = booking;
                ViewBag.CustomerId = customer.Id;
                return View();
            }
        }

        // GET: Booking/PaymentSuccess - Trang thanh toán thành công
        public async Task<IActionResult> PaymentSuccess(long? bookingId)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            if (!bookingId.HasValue)
            {
                return RedirectToAction("MyBookings");
            }

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("MyBookings");
            }

            // Lấy booking
            var booking = await _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyBookings");
            }

            return View(booking);
        }

        // GET: Booking/PaymentCancelled - Trang thanh toán bị hủy
        public async Task<IActionResult> PaymentCancelled(long? bookingId)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            if (!bookingId.HasValue)
            {
                return RedirectToAction("MyBookings");
            }

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("MyBookings");
            }

            // Lấy booking
            var booking = await _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id);

            // Cho phép hiển thị thông tin ngay cả khi booking null
            return View(booking);
        }

        // GET: Booking/PayOSReturn - Callback từ PayOS sau khi thanh toán
        [HttpGet]
        public async Task<IActionResult> PayOSReturn(long? bookingId, string? status, int? orderCode, string? code, bool? cancel)
        {
            _logger.LogInformation("PayOSReturn - Begin, BookingId: {BookingId}, Status: {Status}, OrderCode: {OrderCode}, Code: {Code}, Cancel: {Cancel}",
                bookingId, status, orderCode, code, cancel);

            // Khai báo localhostUrl một lần để dùng chung
            var localhostUrl = "http://localhost:5209";

            // Nếu không có bookingId, thử extract từ orderCode
            // Ưu tiên bookingId dài hơn (thử từ dài đến ngắn)
            if (!bookingId.HasValue && orderCode.HasValue)
            {
                var orderCodeStr = orderCode.Value.ToString();
                // Thử extract bookingId từ orderCode (bỏ 6 chữ số cuối)
                // Thử từ dài đến ngắn để ưu tiên bookingId dài hơn
                for (int len = orderCodeStr.Length - 6; len >= 1; len--)
                {
                    var possibleBookingIdStr = orderCodeStr.Substring(0, len);
                    if (long.TryParse(possibleBookingIdStr, out long extractedBookingId))
                    {
                        var testBooking = await _context.Bookings
                            .FirstOrDefaultAsync(b => b.Id == extractedBookingId);
                        if (testBooking != null)
                        {
                            bookingId = extractedBookingId;
                            _logger.LogInformation("PayOSReturn - Extracted BookingId {BookingId} from OrderCode {OrderCode}",
                                extractedBookingId, orderCode.Value);
                            break;
                        }
                    }
                }
            }

            if (!bookingId.HasValue)
            {
                _logger.LogWarning("PayOSReturn - Cannot find bookingId. Status: {Status}, OrderCode: {OrderCode}, Code: {Code}",
                    status, orderCode, code);
                // Nếu có status = PAID hoặc code = 00, vẫn redirect về success (webhook sẽ xử lý)
                if (status == "PAID" || status == "success" || code == "00")
                {
                    var redirectUrl = $"{localhostUrl}/Booking/PayOSReturnLocal?status=success";
                    return Redirect(redirectUrl);
                }
                TempData["ErrorMessage"] = "Không tìm thấy mã đơn hàng.";
                var redirectUrl2 = $"{localhostUrl}/Booking/PayOSReturnLocal?status=cancelled";
                return Redirect(redirectUrl2);
            }

            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("PaymentCancelled", new { bookingId = bookingId });
            }

            // Lấy booking - nếu thanh toán thành công, không cần kiểm tra CustomerId
            // vì có thể user đã logout hoặc session khác
            var booking = await _context.Bookings
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == bookingId.Value);

            // Nếu không tìm thấy booking, thử tìm không cần CustomerId
            if (booking == null)
            {
                _logger.LogWarning("PayOSReturn - Booking {BookingId} not found in database. Status: {Status}, Code: {Code}",
                    bookingId, status, code);
                // Nếu thanh toán thành công nhưng không tìm thấy booking, vẫn redirect về success
                // Webhook sẽ xử lý cập nhật trạng thái
                if (status == "PAID" || status == "success" || code == "00")
                {
                    var redirectUrl = $"{localhostUrl}/Booking/PayOSReturnLocal?bookingId={bookingId.Value}&status=success";
                    return Redirect(redirectUrl);
                }
                // Nếu không phải thành công, redirect về cancelled
                var redirectUrl2 = $"{localhostUrl}/Booking/PayOSReturnLocal?bookingId={bookingId.Value}&status=cancelled";
                return Redirect(redirectUrl2);
            }

            // Kiểm tra xem booking có thuộc về customer hiện tại không
            // Nếu không, vẫn cho phép xử lý nếu thanh toán thành công (có thể do session khác)
            bool isDifferentCustomer = booking.CustomerId != customer.Id;
            if (isDifferentCustomer)
            {
                _logger.LogWarning("PayOSReturn - Booking {BookingId} belongs to different customer. Current CustomerId: {CurrentCustomerId}, Booking CustomerId: {BookingCustomerId}. Status: {Status}",
                    bookingId, customer.Id, booking.CustomerId, status);
            }

            // PayOS sẽ gửi webhook để cập nhật trạng thái
            // Nhưng cũng cập nhật ngay khi nhận callback để đảm bảo status được cập nhật kịp thời
            // Kiểm tra status từ URL callback
            // Redirect về localhost:5209 để tránh ngrok warning page
            if (status == "PAID" || status == "success" || code == "00")
            {
                // Cập nhật trạng thái booking ngay khi nhận được callback thành công
                // Cập nhật ngay cả khi CustomerId không khớp (có thể do session khác)
                // Không cần đợi webhook (webhook sẽ xử lý lại nhưng không sao)
                if (booking.StatusId == 1 || booking.StatusId == 2) // Pending hoặc Approved
                {
                    var bankedStatusId = await GetStatusIdAsync("banked");
                    if (bankedStatusId.HasValue)
                    {
                        booking.StatusId = bankedStatusId.Value;
                        booking.UpdatedDate = DateTime.Now;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation("PayOSReturn - Successfully updated booking {BookingId} to StatusId: {StatusId} (banked) after successful payment callback. IsDifferentCustomer: {IsDifferentCustomer}",
                            booking.Id, booking.StatusId, isDifferentCustomer);
                    }
                }
                else
                {
                    _logger.LogInformation("PayOSReturn - Booking {BookingId} already processed, StatusId: {StatusId}",
                        booking.Id, booking.StatusId);
                }

                // Thanh toán thành công - redirect về localhost với thông báo thành công
                _logger.LogInformation("PayOSReturn - Payment successful for booking {BookingId}, redirecting to localhost", bookingId.Value);
                var redirectUrl = $"{localhostUrl}/Booking/PayOSReturnLocal?bookingId={bookingId.Value}&status=success";
                return Redirect(redirectUrl);
            }
            else if (status == "CANCELLED" || status == "cancelled" || status == "CANCEL" || cancel == true)
            {
                // Thanh toán bị hủy - redirect về localhost với thông báo hủy
                _logger.LogInformation("PayOSReturn - Payment cancelled for booking {BookingId}, redirecting to localhost", bookingId.Value);
                var redirectUrl = $"{localhostUrl}/Booking/PayOSReturnLocal?bookingId={bookingId.Value}&status=cancelled";
                return Redirect(redirectUrl);
            }
            else
            {
                // Trạng thái không xác định - redirect về localhost với thông báo đang xử lý
                _logger.LogWarning("PayOSReturn - Unknown status '{Status}' for booking {BookingId}, redirecting to localhost", status, bookingId.Value);
                var redirectUrl = $"{localhostUrl}/Booking/PayOSReturnLocal?bookingId={bookingId.Value}&status=pending";
                return Redirect(redirectUrl);
            }
        }

        // GET: Booking/PayOSReturnLocal - Xử lý redirect từ ngrok về localhost sau khi thanh toán
        [HttpGet]
        public async Task<IActionResult> PayOSReturnLocal(long? bookingId, string? status)
        {
            _logger.LogInformation("PayOSReturnLocal - Begin, BookingId: {BookingId}, Status: {Status}", bookingId, status);

            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập.";
                return RedirectToAction("Login", "Account");
            }

            // Xử lý theo status
            if (status == "success")
            {
                TempData["SuccessMessage"] = "Thanh toán thành công! Đơn hàng của bạn đã được xác nhận.";
                _logger.LogInformation("PayOSReturnLocal - Payment successful for booking {BookingId}", bookingId);
            }
            else if (status == "cancelled")
            {
                TempData["ErrorMessage"] = "Thanh toán đã bị hủy. Vui lòng thử lại nếu bạn muốn tiếp tục thanh toán.";
                _logger.LogInformation("PayOSReturnLocal - Payment cancelled for booking {BookingId}", bookingId);
            }
            else
            {
                TempData["InfoMessage"] = "Đang xử lý thanh toán. Vui lòng chờ xác nhận...";
                _logger.LogInformation("PayOSReturnLocal - Payment pending for booking {BookingId}", bookingId);
            }

            // Redirect về MyBookings
            return RedirectToAction("MyBookings");
        }

        // GET: Booking/VnpayReturn - Callback từ VNPay sau khi thanh toán
        public async Task<IActionResult> VnpayReturn()
        {
            _logger.LogInformation("VNPay Return - Begin, URL: {RawUrl}", Request.QueryString);

            var result = await _vnpayService.ProcessReturnAsync(Request.Query);

            if (result.IsSuccess)
            {
                // Thanh toán thành công
                if (long.TryParse(result.BookingNumber, out long bookingId))
                {
                    _logger.LogInformation("VNPay Return - Payment successful for booking {BookingId}", bookingId);
                    return RedirectToAction("PaymentSuccess", new { bookingId = bookingId });
                }

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("MyBookings");
            }
            else
            {
                // Thanh toán thất bại hoặc bị hủy
                if (!string.IsNullOrEmpty(result.BookingNumber) && long.TryParse(result.BookingNumber, out long bookingId))
                {
                    _logger.LogWarning("VNPay Return - Payment failed/cancelled for booking {BookingId}. Message: {Message}",
                        bookingId, result.Message);
                    return RedirectToAction("PaymentCancelled", new { bookingId = bookingId });
                }

                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("MyBookings");
            }
        }

        // GET: Booking/OnlinePayment - Trang thanh toán hoặc xử lý callback từ PayOS
        [HttpGet]
        public async Task<IActionResult> OnlinePayment(long? bookingId, string? status, string? code, string? id, bool? cancel, int? orderCode)
        {
            // Nếu có query parameters từ PayOS callback, xử lý như PayOSReturn
            if (!string.IsNullOrEmpty(status) || !string.IsNullOrEmpty(code) || orderCode.HasValue)
            {
                _logger.LogInformation("OnlinePayment - Received PayOS callback. Status: {Status}, Code: {Code}, OrderCode: {OrderCode}, Id: {Id}, Cancel: {Cancel}",
                    status, code, orderCode, id, cancel);

                // Lấy bookingId từ query parameter id hoặc bookingId
                long? actualBookingId = bookingId;
                if (!actualBookingId.HasValue && !string.IsNullOrEmpty(id))
                {
                    // Thử parse id nếu là số
                    if (long.TryParse(id, out long parsedId))
                    {
                        actualBookingId = parsedId;
                    }
                }

                // Nếu có orderCode nhưng chưa có bookingId, thử extract bookingId từ orderCode
                // PayOS orderCode format: bookingId + timestamp (6 chữ số cuối)
                // Ví dụ: bookingId=26, timestamp=1234567890 => orderCode = 26123456
                // Ưu tiên bookingId dài hơn (thử từ dài đến ngắn)
                if (!actualBookingId.HasValue && orderCode.HasValue)
                {
                    var orderCodeStr = orderCode.Value.ToString();
                    // Thử extract bookingId bằng cách lấy phần đầu (bỏ 6 chữ số cuối)
                    // Tìm bookingId hợp lệ bằng cách thử từ dài đến ngắn (ưu tiên bookingId dài hơn)
                    for (int len = orderCodeStr.Length - 6; len >= 1; len--)
                    {
                        var possibleBookingIdStr = orderCodeStr.Substring(0, len);
                        if (long.TryParse(possibleBookingIdStr, out long extractedBookingId))
                        {
                            // Kiểm tra xem booking có tồn tại không
                            var testBooking = await _context.Bookings
                                .FirstOrDefaultAsync(b => b.Id == extractedBookingId);
                            if (testBooking != null)
                            {
                                actualBookingId = extractedBookingId;
                                _logger.LogInformation("OnlinePayment - Extracted BookingId {BookingId} from OrderCode {OrderCode}",
                                    extractedBookingId, orderCode.Value);
                                break;
                            }
                        }
                    }
                }

                // Nếu không tìm thấy bookingId, redirect về MyBookings với thông báo
                if (!actualBookingId.HasValue)
                {
                    _logger.LogWarning("OnlinePayment - Cannot extract BookingId from callback. Status: {Status}, OrderCode: {OrderCode}",
                        status, orderCode);
                    TempData["InfoMessage"] = "Đang xử lý thanh toán. Vui lòng kiểm tra lại sau vài phút.";
                    return RedirectToAction("MyBookings");
                }

                // Redirect đến PayOSReturn để xử lý
                return RedirectToAction("PayOSReturn", new { bookingId = actualBookingId, status = status, orderCode = orderCode });
            }

            // Nếu không có callback parameters, hiển thị trang thanh toán bình thường
            if (bookingId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã đơn hàng.";
                return RedirectToAction("MyBookings");
            }

            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông tin thanh toán.";
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

            // Lấy booking với đầy đủ thông tin
            var booking = await _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("MyBookings");
            }

            // Kiểm tra booking phải ở trạng thái Approved (StatusId = 2) hoặc đã thanh toán (StatusId = 12)
            if (booking.StatusId != 2 && booking.StatusId != 12)
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

            // Kiểm tra đã upload ảnh CCCD và thẻ sinh viên chưa (chỉ kiểm tra khi StatusId = 2, chưa thanh toán)
            if (booking.StatusId == 2 && (string.IsNullOrEmpty(booking.IdNoUrl) || string.IsNullOrEmpty(booking.StudentUrl)))
            {
                TempData["ErrorMessage"] = "Vui lòng upload ảnh CCCD và thẻ sinh viên trước khi thanh toán.";
                return RedirectToAction("UploadDocuments", new { id = booking.Id });
            }

            // Kiểm tra thời gian hợp lệ - chỉ cho phép thanh toán khi đơn đang trong thời gian hợp lệ (chỉ kiểm tra khi StatusId = 2, chưa thanh toán)
            if (booking.StatusId == 2)
            {
                var now = DateTime.Now;
                if (booking.EndTime < now)
                {
                    TempData["ErrorMessage"] = $"Đơn hàng này đã hết thời hạn thanh toán. Thời gian thuê đã kết thúc vào {booking.EndTime:dd/MM/yyyy HH:mm}.";
                    return RedirectToAction("MyBookings");
                }
                if (booking.StartTime > now)
                {
                    TempData["ErrorMessage"] = $"Đơn hàng này chưa đến thời gian thanh toán. Thời gian thuê bắt đầu từ {booking.StartTime:dd/MM/yyyy HH:mm}.";
                    return RedirectToAction("MyBookings");
                }
            }

            ViewBag.Booking = booking;
            ViewBag.CustomerId = customer.Id;

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

        // GET: Booking/PayWithVNPay - Tạo payment URL và redirect đến VNPay
        [HttpGet]
        public async Task<IActionResult> PayWithVNPay(long? bookingId)
        {
            if (!bookingId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã đơn hàng.";
                return RedirectToAction("OnlinePayment");
            }

            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("OnlinePayment");
            }

            // Lấy booking
            var booking = await _context.Bookings
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập đơn hàng này.";
                return RedirectToAction("OnlinePayment");
            }

            // Kiểm tra trạng thái đơn hàng
            if (booking.StatusId != 2 && booking.StatusId != 12)
            {
                TempData["ErrorMessage"] = "Đơn hàng này không ở trạng thái đã duyệt, không thể thanh toán.";
                return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
            }

            // Kiểm tra nếu đã thanh toán
            if (booking.StatusId == 12)
            {
                TempData["SuccessMessage"] = "Đơn hàng này đã được thanh toán thành công.";
                return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
            }

            try
            {
                // Lấy IP address
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1")
                {
                    ipAddress = "127.0.0.1";
                }

                // Tạo payment URL
                var paymentUrl = await _vnpayService.CreatePaymentUrlAsync(bookingId.Value, ipAddress);

                _logger.LogInformation("VNPay Payment - Created payment URL for booking {BookingId}, Redirecting to VNPay", bookingId.Value);

                // Redirect đến VNPay
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay Payment - Error creating payment URL for booking {BookingId}", bookingId.Value);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo liên kết thanh toán. Vui lòng thử lại sau.";
                return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
            }
        }

        // GET: Booking/PayWithPayOS - Tạo payment link và redirect đến PayOS
        [HttpGet]
        public async Task<IActionResult> PayWithPayOS(long? bookingId)
        {
            if (!bookingId.HasValue)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã đơn hàng.";
                return RedirectToAction("OnlinePayment");
            }

            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("OnlinePayment");
            }

            // Lấy booking
            var booking = await _context.Bookings
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập đơn hàng này.";
                return RedirectToAction("OnlinePayment");
            }

            // Kiểm tra trạng thái đơn hàng
            if (booking.StatusId != 2 && booking.StatusId != 12)
            {
                TempData["ErrorMessage"] = "Đơn hàng này không ở trạng thái đã duyệt, không thể thanh toán.";
                return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
            }

            // Kiểm tra đã upload ảnh CCCD và thẻ sinh viên chưa (chỉ kiểm tra khi StatusId = 2, chưa thanh toán)
            if (booking.StatusId == 2 && (string.IsNullOrEmpty(booking.IdNoUrl) || string.IsNullOrEmpty(booking.StudentUrl)))
            {
                TempData["ErrorMessage"] = "Vui lòng upload ảnh CCCD và thẻ sinh viên trước khi thanh toán.";
                return RedirectToAction("UploadDocuments", new { id = bookingId.Value });
            }

            // Kiểm tra thời gian hợp lệ - chỉ cho phép thanh toán khi đơn đang trong thời gian hợp lệ (chỉ kiểm tra khi StatusId = 2, chưa thanh toán)
            if (booking.StatusId == 2)
            {
                var now = DateTime.Now;
                if (booking.EndTime < now)
                {
                    TempData["ErrorMessage"] = $"Đơn hàng này đã hết thời hạn thanh toán. Thời gian thuê đã kết thúc vào {booking.EndTime:dd/MM/yyyy HH:mm}.";
                    return RedirectToAction("MyBookings");
                }
                if (booking.StartTime > now)
                {
                    TempData["ErrorMessage"] = $"Đơn hàng này chưa đến thời gian thanh toán. Thời gian thuê bắt đầu từ {booking.StartTime:dd/MM/yyyy HH:mm}.";
                    return RedirectToAction("MyBookings");
                }
            }

            // Kiểm tra nếu đã thanh toán
            if (booking.StatusId == 12)
            {
                TempData["SuccessMessage"] = "Đơn hàng này đã được thanh toán thành công.";
                return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
            }

            try
            {
                // Tạo payment link
                var paymentUrl = await _payOSService.CreatePaymentLinkAsync(bookingId.Value);

                _logger.LogInformation("PayOS Payment - Created payment link for booking {BookingId}, Redirecting to PayOS", bookingId.Value);

                // Redirect đến PayOS
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOS Payment - Error creating payment link for booking {BookingId}", bookingId.Value);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo liên kết thanh toán. Vui lòng thử lại sau.";
                return RedirectToAction("OnlinePayment", new { bookingId = bookingId.Value });
            }
        }

        // POST: Booking/PayOSWebhook - Webhook để nhận thông báo thanh toán từ PayOS
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Cho phép webhook từ bên ngoài
        public async Task<IActionResult> PayOSWebhook()
        {
            try
            {
                // Log raw request để debug
                _logger.LogInformation("PayOSWebhook - Received request. ContentType: {ContentType}, Method: {Method}, Path: {Path}",
                    Request.ContentType, Request.Method, Request.Path);

                // Đọc raw body
                Request.EnableBuffering();
                Request.Body.Position = 0;
                using var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
                var rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
                _logger.LogInformation("PayOSWebhook - Raw body: {RawBody}", rawBody);

                // Parse webhook body từ PayOS
                WebhookType? webhookBody = null;
                if (!string.IsNullOrEmpty(rawBody))
                {
                    try
                    {
                        webhookBody = System.Text.Json.JsonSerializer.Deserialize<WebhookType>(
                            rawBody,
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "PayOSWebhook - Failed to parse JSON body");
                        return BadRequest(new { success = false, message = "Invalid request format" });
                    }
                }

                if (webhookBody == null)
                {
                    _logger.LogWarning("PayOSWebhook - Webhook body is null");
                    return BadRequest(new { success = false, message = "Webhook body is null" });
                }

                // Verify webhook data bằng thư viện PayOS
                WebhookData webhookData;
                try
                {
                    webhookData = _payOSService.VerifyWebhookData(webhookBody);
                    _logger.LogInformation("PayOSWebhook - Webhook verified successfully. OrderCode: {OrderCode}, Amount: {Amount}",
                        webhookData.orderCode, webhookData.amount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PayOSWebhook - Webhook verification failed. This might be a test webhook from PayOS Dashboard");
                    // PayOS có thể gửi test webhook không có signature hợp lệ
                    // Trả về 200 OK để PayOS không retry, nhưng log lỗi
                    return Ok(new { success = false, message = "Webhook verification failed - might be test webhook" });
                }

                // Extract bookingId - ưu tiên từ description vì có bookingId rõ ràng
                // Description format: "Thanh toan don hang {bookingId}"
                long bookingId = 0;
                bool foundBooking = false;

                if (!string.IsNullOrEmpty(webhookData.description))
                {
                    var descParts = webhookData.description.Split(' ');
                    foreach (var part in descParts)
                    {
                        if (long.TryParse(part, out long descBookingId))
                        {
                            var testBooking = await _context.Bookings
                                .FirstOrDefaultAsync(b => b.Id == descBookingId);
                            if (testBooking != null)
                            {
                                bookingId = descBookingId;
                                foundBooking = true;
                                _logger.LogInformation("PayOSWebhook - Extracted BookingId {BookingId} from Description: {Description}",
                                    bookingId, webhookData.description);
                                break;
                            }
                        }
                    }
                }

                // Nếu không tìm thấy từ description, thử extract từ orderCode
                // PayOS orderCode format: bookingId + timestamp (6 chữ số cuối)
                if (!foundBooking)
                {
                    var orderCodeStr = webhookData.orderCode.ToString();
                    // Thử extract bookingId bằng cách bỏ 6 chữ số cuối
                    // Tìm bookingId hợp lệ bằng cách thử từ dài đến ngắn (ưu tiên bookingId dài hơn)
                    for (int len = orderCodeStr.Length - 6; len >= 1; len--)
                    {
                        var possibleBookingIdStr = orderCodeStr.Substring(0, len);
                        if (long.TryParse(possibleBookingIdStr, out long extractedBookingId))
                        {
                            var testBooking = await _context.Bookings
                                .FirstOrDefaultAsync(b => b.Id == extractedBookingId);
                            if (testBooking != null)
                            {
                                bookingId = extractedBookingId;
                                foundBooking = true;
                                _logger.LogInformation("PayOSWebhook - Extracted BookingId {BookingId} from OrderCode {OrderCode}",
                                    bookingId, webhookData.orderCode);
                                break;
                            }
                        }
                    }
                }

                _logger.LogInformation("PayOSWebhook - OrderCode: {OrderCode}, Extracted BookingId: {BookingId}, Amount: {Amount}, Description: {Description}",
                    webhookData.orderCode, bookingId, webhookData.amount, webhookData.description);

                if (!foundBooking)
                {
                    _logger.LogWarning("PayOSWebhook - Cannot extract BookingId from OrderCode {OrderCode}, Description: {Description}",
                        webhookData.orderCode, webhookData.description);
                    return Ok(new { success = false, message = $"Cannot extract BookingId from OrderCode {webhookData.orderCode}" });
                }

                var booking = await _context.Bookings
                    .Include(b => b.Status)
                    .FirstOrDefaultAsync(b => b.Id == bookingId);

                if (booking == null)
                {
                    _logger.LogWarning("PayOSWebhook - Booking {BookingId} not found", bookingId);
                    // Trả về Ok thay vì NotFound để PayOS không retry webhook
                    // và ngrok không hiển thị 404
                    return Ok(new { success = false, message = $"Booking {bookingId} not found" });
                }

                // Kiểm tra số tiền
                var bookingAmount = (long)(booking.TotalPrice ?? 0);
                var receivedAmount = webhookData.amount;

                // Xử lý trường hợp TotalPrice = 0/null nhưng đã nhận được tiền
                if (bookingAmount == 0 && receivedAmount > 0)
                {
                    // Cập nhật TotalPrice từ số tiền đã nhận
                    booking.TotalPrice = receivedAmount;
                    _logger.LogInformation("PayOSWebhook - Booking TotalPrice was 0/null, auto-updated to received amount {ReceivedAmount}",
                        receivedAmount);
                }
                else
                {
                    // Cho phép sai số nhỏ (có thể do làm tròn)
                    if (Math.Abs(bookingAmount - receivedAmount) > 1000) // Cho phép sai số 1000 VND
                    {
                        _logger.LogWarning("PayOSWebhook - Amount mismatch. Booking: {BookingAmount}, Received: {ReceivedAmount}",
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

                        _logger.LogInformation("PayOSWebhook - Successfully updated booking {BookingId} to StatusId: {StatusId} (banked) after successful payment. PayOS OrderCode: {OrderCode}",
                            booking.Id, booking.StatusId, webhookData.orderCode);

                        return Ok(new { success = true, message = "Payment confirmed and booking approved", bookingId = booking.Id });
                    }
                    else
                    {
                        _logger.LogError("PayOSWebhook - Banked status not found in database");
                        return StatusCode(500, new { success = false, message = "Banked status not found" });
                    }
                }
                else
                {
                    _logger.LogInformation("PayOSWebhook - Booking {BookingId} already processed, StatusId: {StatusId}",
                        booking.Id, booking.StatusId);
                    return Ok(new { success = true, message = "Already confirmed", bookingId = booking.Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PayOSWebhook - Error processing webhook. Error: {Error}", ex.Message);
                return StatusCode(500, new { success = false, message = "Internal server error", error = ex.Message });
            }
        }

        // POST: Booking/BankTransferWebhook - Webhook để nhận thông báo chuyển khoản từ Sepay
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous] // Cho phép webhook từ bên ngoài
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

        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }

    }

    // Model cho PayOS Webhook Request
    public class PayOSWebhookRequest
    {
        public int Code { get; set; } // 0 = success (khi là int)
        public string? CodeString { get; set; } // "00" = success (khi là string)
        public string? Desc { get; set; } // "success" khi thanh toán thành công
        public PayOSWebhookData? Data { get; set; }
        public string? Signature { get; set; } // Chữ ký để verify
    }

    public class PayOSWebhookData
    {
        public string? OrderCode { get; set; } // Mã đơn hàng (khi là string)
        public long? OrderCodeLong { get; set; } // Mã đơn hàng (khi là int/long)
        public long Amount { get; set; } // Số tiền (VND)
        public string? Description { get; set; } // Mô tả
        public string? AccountNumber { get; set; }
        public string? TransactionId { get; set; }
        public string? Reference { get; set; }
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
        public string? Accumulated { get; set; }
        public long? Id { get; set; } // Transaction ID từ Sepay
    }
}

