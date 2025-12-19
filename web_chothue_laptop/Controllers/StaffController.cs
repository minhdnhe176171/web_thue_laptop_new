using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace web_chothue_laptop.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<StaffController> _logger;

        public StaffController(Swp391LaptopContext context, ILogger<StaffController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // TRANG 1: QUẢN LÝ ĐƠN THUÊ (Mặc định)
        // ==========================================
        public async Task<IActionResult> Index(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString; // Giữ lại từ khóa tìm kiếm

            var query = _context.Bookings
                .Include(b => b.Customer).Include(b => b.Laptop)
                .Where(b => b.StatusId == 1); // Trạng thái Pending

            // --- ĐOẠN FILTER ---
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Customer.LastName.Contains(searchString)
                                      || b.Customer.FirstName.Contains(searchString)
                                      || b.Id.ToString().Contains(searchString));
            }

            // Lưu tổng số lượng để hiển thị trên tiêu đề (vì PaginatedList chỉ chứa 5 dòng)
            ViewData["TotalCount"] = await query.CountAsync();

            query = query.OrderByDescending(b => b.CreatedDate);

            // --- ĐOẠN PHÂN TRANG ---
            int pageSize = 5;
            return View(await PaginatedList<Booking>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
        // ==========================================
        // TRANG 2: MÁY CHỜ KIỂM TRA (Action Mới)
        // ==========================================
        public async Task<IActionResult> LaptopRequests(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            // Lấy danh sách laptop có TechnicalTicket được Technical duyệt (StatusId = 2)
            var approvedTicketLaptopIds = await _context.TechnicalTickets
                .Where(t => t.StatusId == 2 && t.BookingId == null) // Technical approved, không phải ticket của customer
                .Select(t => t.LaptopId)
                .ToListAsync();

            var query = _context.Laptops
                .Include(l => l.Student)
                .Include(l => l.Brand)
                .Where(l => l.StatusId == 1 && approvedTicketLaptopIds.Contains(l.Id)); // Chỉ lấy laptop Pending có ticket đã duyệt

            // --- FILTER ---
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l =>
                    (l.Name != null && l.Name.Contains(searchString)) ||
                    (l.Student != null && l.Student.LastName != null && l.Student.LastName.Contains(searchString)) ||
                    (l.Student != null && l.Student.FirstName != null && l.Student.FirstName.Contains(searchString))
                );
            }
            ViewData["TotalCount"] = await query.CountAsync(); // Lưu tổng số

            query = query.OrderByDescending(l => l.UpdatedDate);

            // --- PHÂN TRANG ---
            int pageSize = 5;
            return View(await PaginatedList<Laptop>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // ==========================================
        // CÁC HÀM XỬ LÝ BOOKING (Quay về Index)
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
                
            if (booking != null)
            {
                // Kiểm tra blacklist
                if (booking.Customer?.BlackList == true)
                {
                    TempData["WarningMessage"] = $"Cảnh báo: Customer này đang trong blacklist. Đơn thuê đã được duyệt nhưng vui lòng cẩn thận!";
                }
                
                booking.StatusId = 2; // Approved
                booking.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã duyệt đơn thuê thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(long bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.StatusId = 3; // Rejected
                booking.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = "Đã từ chối đơn thuê.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // CÁC HÀM XỬ LÝ LAPTOP (Quay về LaptopRequests)
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToTechnical(long laptopId)
        {
            var laptop = await _context.Laptops.FindAsync(laptopId);
            if (laptop != null)
            {
                var existingTicket = await _context.TechnicalTickets
                    .FirstOrDefaultAsync(t => t.LaptopId == laptopId && t.StatusId == 1);

                if (existingTicket == null)
                {
                    var newTicket = new TechnicalTicket
                    {
                        LaptopId = laptopId,
                        StaffId = 1, // Tạm fix cứng, sau này lấy từ User.Identity
                        StatusId = 1,
                        Description = "Yêu cầu kiểm tra chất lượng máy nhập kho mới.",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };
                    _context.TechnicalTickets.Add(newTicket);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã gửi yêu cầu kiểm tra cho Technical!";
                }
                else
                {
                    TempData["WarningMessage"] = "Máy này đã được gửi đi rồi!";
                }
            }
            // QUAN TRỌNG: Quay lại trang LaptopRequests thay vì Index
            return RedirectToAction(nameof(LaptopRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishLaptop(long laptopId)
        {
            var laptop = await _context.Laptops.FindAsync(laptopId);
            if (laptop != null && laptop.StatusId == 1) // Kiểm tra StatusId = 1 (Pending) thay vì 2
            {
                laptop.StatusId = 9; // Available - Đưa lên web
                laptop.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã niêm yết máy thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Laptop này không thể niêm yết (trạng thái không hợp lệ).";
            }
            // QUAN TRỌNG: Quay lại trang LaptopRequests thay vì Index
            return RedirectToAction(nameof(LaptopRequests));
        }

        // ==========================================
        // QUẢN LÝ GIAO MÁY (DELIVERIES)
        // ==========================================

        /// <summary>
        /// Danh sách booking cần giao máy
        /// </summary>
        public async Task<IActionResult> Deliveries(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop).ThenInclude(l => l.Brand)
                .Include(b => b.Laptop).ThenInclude(l => l.LaptopDetails)
                .Include(b => b.Status)
                .Where(b => b.Status.StatusName.ToLower() == "approved" &&
                           b.StartTime <= DateTime.Today.AddDays(7));

            // --- FILTER ---
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Customer.LastName.Contains(searchString)
                                      || b.Customer.FirstName.Contains(searchString)
                                      || b.Id.ToString().Contains(searchString));
            }

            query = query.OrderBy(b => b.StartTime);

            // --- PHÂN TRANG ---
            int pageSize = 5;
            return View(await PaginatedList<Booking>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        /// <summary>
        /// Hiển thị form tạo phiếu giao máy
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateDelivery(long? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction(nameof(Deliveries));
            }

            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.LaptopDetails)
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction(nameof(Deliveries));
            }

            // Kiểm tra booking đã được approve chưa
            if (booking.Status?.StatusName?.ToLower() != "approved")
            {
                TempData["ErrorMessage"] = "Booking này chưa được approve hoặc đã giao máy rồi!";
                return RedirectToAction(nameof(Deliveries));
            }

            // Kiểm tra xem đã có phiếu giao máy cho booking này chưa
            var existingReceipt = await _context.BookingReceipts
                .FirstOrDefaultAsync(br => br.BookingId == booking.Id);

            if (existingReceipt != null)
            {
                TempData["ErrorMessage"] = "Booking này đã được giao máy rồi!";
                return RedirectToAction(nameof(Deliveries));
            }

            var detail = booking.Laptop?.LaptopDetails?.FirstOrDefault();

            var model = new DeliveryViewModel
            {
                BookingId = booking.Id,
                CustomerName = $"{booking.Customer?.LastName} {booking.Customer?.FirstName}",
                LaptopName = booking.Laptop?.Name,
                StartDate = booking.StartTime,
                EndDate = booking.EndTime,
                TotalPrice = booking.TotalPrice,
                Cpu = detail?.Cpu,
                RamSize = detail?.RamSize,
                Storage = detail?.Storage,
                Gpu = detail?.Gpu,
                ScreenSize = detail?.ScreenSize,
                Os = detail?.Os,
                // Mặc định tất cả điều kiện đều OK
                ScreenCondition = true,
                KeyboardCondition = true,
                MouseCondition = true,
                ChargerCondition = true,
                BodyCondition = true,
                BatteryLevel = 100
            };

            return View(model);
        }

        /// <summary>
        /// Xử lý tạo phiếu giao máy
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDelivery(DeliveryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra booking tồn tại
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop)
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction(nameof(Deliveries));
            }

            // Kiểm tra trạng thái
            if (booking.Status?.StatusName?.ToLower() != "approved")
            {
                TempData["ErrorMessage"] = "Booking này không thể giao máy (chưa approve hoặc đã giao).";
                return RedirectToAction(nameof(Deliveries));
            }
            // Kiểm tra đã giao chưa
            var existingReceipt = await _context.BookingReceipts
                .FirstOrDefaultAsync(br => br.BookingId == booking.Id);

            if (existingReceipt != null)
            {
                TempData["ErrorMessage"] = "Booking này đã được giao máy rồi!";
                return RedirectToAction(nameof(Deliveries));
            }
            try
            {
                // Lấy Staff ID từ session (hoặc tạm fix cứng)
                var userId = HttpContext.Session.GetString("UserId");
                long staffId = 1; // Mặc định

                if (!string.IsNullOrEmpty(userId))
                {
                    var userIdLong = long.Parse(userId);
                    var staff = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == userIdLong);
                    if (staff != null)
                    {
                        staffId = staff.Id;
                    }
                }

                // Upload ảnh nếu có (tạm bỏ qua, có thể dùng Cloudinary)
            //    string? imageUrl = null;
                if (model.ConditionImage != null && model.ConditionImage.Length > 0)
                {
                    // TODO: Upload to Cloudinary or save to wwwroot
                    _logger.LogInformation($"Image uploaded: {model.ConditionImage.FileName}");
                }

                // Tạo phiếu giao máy (BookingReceipt)
                var receipt = new BookingReceipt
                {
                    BookingId = booking.Id,
                    CustomerId = booking.CustomerId,
                    StaffId = staffId,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    TotalPrice = booking.TotalPrice ?? 0,
                    LateFee = 0, // Chưa có phí trễ
                    LateMinutes = 0,
                    CreatedDate = DateTime.Now
                };

                _context.BookingReceipts.Add(receipt);

                // Cập nhật trạng thái booking sang "Rented" (đang thuê)
                var rentedStatus = await _context.Statuses
                    .FirstOrDefaultAsync(s => s.StatusName.ToLower() == "rented");

                if (rentedStatus != null)
                {
                    booking.StatusId = rentedStatus.Id;
                    booking.UpdatedDate = DateTime.Now;
                }

                // Lưu ghi chú vào database (có thể thêm trường Notes vào BookingReceipt sau)
                // Hoặc lưu vào bảng khác nếu cần
                _logger.LogInformation($"Delivery created for Booking #{booking.Id}");
                _logger.LogInformation($"Conditions - Screen: {model.ScreenCondition}, Keyboard: {model.KeyboardCondition}, Battery: {model.BatteryLevel}%");
                if (!string.IsNullOrEmpty(model.Notes))
                {
                    _logger.LogInformation($"Notes: {model.Notes}");
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tạo phiếu giao máy thành công cho Booking #{booking.Id}!";
                return RedirectToAction(nameof(Deliveries));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating delivery receipt");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo phiếu giao máy. Vui lòng thử lại.";
                return View(model);
            }
        }
        // ==========================================
        // GIAO MÁY NHANH (QUICK HANDOVER) - Mới thêm
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> QuickHandover(long? searchBookingId)
        {
            // Nếu chưa nhập gì thì trả về view rỗng
            if (searchBookingId == null)
            {
                return View(null);
            }

            // Tìm kiếm Booking kèm thông tin Customer, Laptop, Status
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Status)
                .FirstOrDefaultAsync(b => b.Id == searchBookingId);

            // Xử lý thông báo lỗi
            if (booking == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng #{searchBookingId}";
                return View(null);
            }

            // Kiểm tra trạng thái (Chỉ cho phép StatusId = 2 là Approved)
            if (booking.StatusId != 2)
            {
                if (booking.StatusId == 10)
                    TempData["WarningMessage"] = $"Đơn hàng #{searchBookingId} đã được giao (Rented) rồi!";
                else
                    TempData["ErrorMessage"] = $"Đơn hàng đang ở trạng thái '{booking.Status?.StatusName}'. Chỉ đơn 'Approved' mới được giao máy.";
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmHandover(long bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);

            // Check lại lần cuối
            if (booking == null || booking.StatusId != 2)
            {
                TempData["ErrorMessage"] = "Đơn hàng không hợp lệ hoặc trạng thái đã thay đổi.";
                return RedirectToAction(nameof(QuickHandover), new { searchBookingId = bookingId });
            }

            try
            {
                // 1. Cập nhật trạng thái Booking -> 10 (Rented)
                booking.StatusId = 10;
                booking.UpdatedDate = DateTime.Now;

                // 2. Tạo phiếu BookingReceipt
                // Lấy Staff ID từ session hoặc mặc định là 1 nếu null
                long staffId = 1;
                var userId = HttpContext.Session.GetString("UserId");
                if (!string.IsNullOrEmpty(userId) && long.TryParse(userId, out long parsedId))
                {
                    // Logic map StaffId của bạn (nếu bảng Staff khác bảng Account)
                    // Ở đây tạm thời để staffId = 1 để code chạy được ngay
                    staffId = 1;
                }

                var receipt = new BookingReceipt
                {
                    BookingId = booking.Id,
                    CustomerId = booking.CustomerId,
                    StaffId = staffId,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    TotalPrice = booking.TotalPrice ?? 0,
                    LateFee = 0,
                    LateMinutes = 0,
                    CreatedDate = DateTime.Now
                };

                _context.BookingReceipts.Add(receipt);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Giao máy thành công đơn #{bookingId}. Đã tạo phiếu thu!";

                // Reset trang để làm đơn tiếp theo
                return RedirectToAction(nameof(QuickHandover));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi giao máy nhanh");
                TempData["ErrorMessage"] = "Lỗi hệ thống khi lưu dữ liệu.";
                return RedirectToAction(nameof(QuickHandover), new { searchBookingId = bookingId });
            }
        }
        // ==========================================
        // QUẢN LÝ MÁY ĐANG ĐƯỢC THUÊ
        // ==========================================

        /// <summary>
        /// Danh sách laptop đang được thuê
        /// </summary>
        public async Task<IActionResult> RentedLaptops(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            // Query gốc
            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop).ThenInclude(l => l.Brand)
                .Include(b => b.Laptop).ThenInclude(l => l.LaptopDetails)
                .Include(b => b.Laptop).ThenInclude(l => l.Student)
                .Include(b => b.Status)
                .Include(b => b.BookingReceipts)
                .Where(b => b.Status.StatusName.ToLower() == "rented");

            // --- TÍNH TOÁN THỐNG KÊ (Trước khi phân trang) ---
            // Vì nếu phân trang rồi thì chỉ tính được trên 5 dòng thôi, nên phải tính riêng ở đây
            var allData = await query.ToListAsync();
            ViewData["Stat_Total"] = allData.Count;
            ViewData["Stat_Expiring"] = allData.Count(b => (b.EndTime - DateTime.Today).Days <= 2 && (b.EndTime - DateTime.Today).Days >= 0);
            ViewData["Stat_Overdue"] = allData.Count(b => b.EndTime < DateTime.Today);
            ViewData["Stat_Revenue"] = allData.Sum(b => b.TotalPrice ?? 0).ToString("#,##0");

            // --- FILTER ---
            // Tạo lại queryable để chạy filter cho bảng
            var displayQuery = query.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                displayQuery = displayQuery.Where(b => b.Customer.LastName.Contains(searchString)
                                                    || b.Customer.FirstName.Contains(searchString)
                                                    || b.Laptop.Name.Contains(searchString));
            }

            displayQuery = displayQuery.OrderBy(b => b.EndTime);

            // --- PHÂN TRANG ---
            int pageSize = 5;
            return View(await PaginatedList<Booking>.CreateAsync(displayQuery.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
        // --- THÊM VÀO StaffController.cs ---

        // 1. Action Xem Chi Tiết
        [HttpGet]
        public async Task<IActionResult> BookingDetails(long id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer) // Người thuê
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.LaptopDetails) // Linh kiện
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Student) // Chủ máy (Student)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction(nameof(Index));
            }

            return View(booking);
        }

        // 2. Action Từ chối kèm lý do
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBookingWithReason(long bookingId, string rejectReason)
        {
            // 1. Kiểm tra nếu chưa nhập lý do
            if (string.IsNullOrWhiteSpace(rejectReason))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối!";
                return RedirectToAction(nameof(BookingDetails), new { id = bookingId });
            }

            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                // 2. Cập nhật trạng thái sang Rejected (3)
                booking.StatusId = 3;
                booking.UpdatedDate = DateTime.Now;

                // 3. LƯU LÝ DO VÀO DATABASE (Quan trọng)
                // Đảm bảo model Booking của bạn đã có cột RejectReason
                booking.RejectReason = rejectReason;

                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = $"Đã từ chối đơn #{bookingId}. Lý do đã được gửi cho khách.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================
        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }
}