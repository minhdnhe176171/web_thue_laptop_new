using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace web_chothue_laptop.Controllers
{
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
        // TRANG CHỦ STAFF DASHBOARD
        // ==========================================
        // 1. SỬA HÀM INDEX
        public async Task<IActionResult> Index()
        {
            // ... (Phần Booking giữ nguyên) ...
            var pendingBookings = await _context.Bookings
                .Include(b => b.Customer).Include(b => b.Laptop)
                .Where(b => b.StatusId == 1).OrderByDescending(b => b.CreatedDate).ToListAsync();

            // SỬA: Lấy cả Status 1 (Mới) và Status 2 (Tech đã duyệt)
            ViewBag.PendingLaptops = await _context.Laptops
                .Include(l => l.Student)
                .Where(l => l.StatusId == 1 || l.StatusId == 2) // <--- THÊM ĐIỀU KIỆN SỐ 2
                .OrderByDescending(l => l.UpdatedDate) // Sắp xếp theo ngày cập nhật để thấy máy mới về
                .ToListAsync();

            return View(pendingBookings);
        }

        // 2. THÊM HÀM MỚI: DUYỆT CHO THUÊ (PUBLISH)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishLaptop(long laptopId)
        {
            var laptop = await _context.Laptops.FindAsync(laptopId);
            if (laptop != null && laptop.StatusId == 2) // Chỉ xử lý máy đã Approved
            {
                laptop.StatusId = 9; // Chuyển sang Available (Sẵn sàng cho thuê)
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã niêm yết máy thành công! Khách hàng có thể thuê ngay bây giờ.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // CÁC HÀM XỬ LÝ
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(long bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.StatusId = 2; // 2: Approved
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
                booking.StatusId = 3; // 3: Rejected
                booking.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = "Đã từ chối đơn thuê.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Gửi máy sang Technical kiểm tra
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToTechnical(long laptopId)
        {
            // 1. Tìm Laptop
            var laptop = await _context.Laptops.FindAsync(laptopId);
            if (laptop != null)
            {
                // 2. Kiểm tra xem đã có Ticket chưa để tránh trùng lặp
                var existingTicket = await _context.TechnicalTickets
                    .FirstOrDefaultAsync(t => t.LaptopId == laptopId && t.StatusId == 1); // Status 1 = Pending

                if (existingTicket == null)
                {
                    // 3. Tạo Ticket mới
                    var newTicket = new TechnicalTicket
                    {
                        LaptopId = laptopId,
                        StaffId = 1, // Tạm fix cứng ID nhân viên
                        StatusId = 1, // ID 1 = Pending (Chờ Technical duyệt)
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
                    TempData["WarningMessage"] = "Máy này đã được gửi đi rồi, đang chờ Technical xử lý!";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // QUẢN LÝ GIAO MÁY (DELIVERIES)
        // ==========================================
        
        /// <summary>
        /// Danh sách booking cần giao máy
        /// </summary>
        public async Task<IActionResult> Deliveries()
        {
            // Lấy danh sách booking đã approved nhưng chưa giao máy
            // (Status = Approved và StartTime trong vòng 7 ngày tới)
            var approvedBookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.LaptopDetails)
                .Include(b => b.Status)
                .Where(b => b.Status.StatusName.ToLower() == "approved" && 
                           b.StartTime <= DateTime.Today.AddDays(7)) // Booking sắp đến hoặc đã đến ngày
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return View(approvedBookings);
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
                string? imageUrl = null;
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
        // QUẢN LÝ MÁY ĐANG ĐƯỢC THUÊ
        // ==========================================
        
        /// <summary>
        /// Danh sách laptop đang được thuê
        /// </summary>
        public async Task<IActionResult> RentedLaptops()
        {
            // Lấy danh sách booking với status "Rented" (đang thuê)
            var rentedBookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.LaptopDetails)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Student)
                .Include(b => b.Status)
                .Include(b => b.BookingReceipts) // Lấy thông tin phiếu giao máy
                .Where(b => b.Status.StatusName.ToLower() == "rented")
                .OrderBy(b => b.EndTime) // Sắp xếp theo ngày trả gần nhất
                .ToListAsync();

            return View(rentedBookings);
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