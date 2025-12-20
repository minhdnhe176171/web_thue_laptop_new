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

                .Include(l => l.TechnicalTickets)
                .Where(l => l.StatusId == 1 || l.StatusId == 2 || l.StatusId == 4); // Mới hoặc Tech đã duyệt

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
                TempData["SuccessMessage"] = "Đã duyệt đơn! Vui lòng thực hiện thanh toán.";                              
                return RedirectToAction(nameof(Deliveries));
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
                // Kiểm tra xem đã có phiếu Pending (Status=1) chưa
                var existingTicket = await _context.TechnicalTickets
                    .FirstOrDefaultAsync(t => t.LaptopId == laptopId && t.StatusId == 1);

                if (existingTicket == null)
                {
                    // NẾU CHƯA CÓ PHIẾU (Hoặc phiếu cũ đã đóng/xong) -> TẠO MỚI
                    var newTicket = new TechnicalTicket
                    {
                        LaptopId = laptopId,
                        StaffId = 1,
                        StatusId = 1, // Tạo ticket Pending để hiện bên Tech
                        Description = "Yêu cầu kiểm tra (Gửi lại).",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    // ❌ XÓA DÒNG NÀY - KHÔNG SET Laptop.StatusId = 4 ở đây
                    // laptop.StatusId = 4;

                    _context.TechnicalTickets.Add(newTicket);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã tạo phiếu kiểm tra mới cho máy {laptop.Name}!";
                }
                else
                {
                    // Nếu đã có phiếu rồi thì chỉ thông báo
                    TempData["WarningMessage"] = "Máy này đã có phiếu chờ bên Technical rồi (Ticket #" + existingTicket.Id + ")";

                    // ❌ XÓA ĐOẠN NÀY - KHÔNG CẦN UPDATE Laptop.StatusId
                    // Fix phụ: Nếu Laptop chưa phải status 4 thì update luôn
                    // if (laptop.StatusId != 4)
                    // {
                    //     laptop.StatusId = 4;
                    //     await _context.SaveChangesAsync();
                    // }
                }
            }
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
                .Include(b => b.BookingReceipts)
                // --- SỬA ĐOẠN NÀY ---
                // Lấy tất cả đơn:
                // 1. Đã duyệt (StatusId = 2) -> Để hiện thị "Chờ thanh toán"
                // 2. Đã thanh toán Online (StatusId = 12) -> Để hiện thị nút "Giao máy"
                .Where(b => b.StatusId == 2 || b.StatusId == 12);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Customer.LastName.Contains(searchString)
                                      || b.Customer.FirstName.Contains(searchString)
                                      || b.Id.ToString().Contains(searchString));
            }

            query = query.OrderBy(b => b.StartTime);
            int pageSize = 5;
            return View(await PaginatedList<Booking>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
        /// <summary>
        /// Hiển thị form tạo phiếu giao máy
        /// </summary>
        // [HttpGet]
        // --- COPY ĐÈ VÀO StaffController.cs ---

        // GET: Staff/CreateDelivery/5
        [HttpGet]
        public async Task<IActionResult> CreateDelivery(long? id)
        {
            // 1. Kiểm tra ID đầu vào
            if (id == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã đơn hàng.";
                return RedirectToAction(nameof(Deliveries));
            }

            // 2. Load dữ liệu Booking từ Database (Kèm thông tin Khách, Laptop, Phiếu thu)
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop).ThenInclude(l => l.Brand)
                .Include(b => b.Laptop).ThenInclude(l => l.LaptopDetails)
                .Include(b => b.Status)
                .Include(b => b.BookingReceipts) // Quan trọng: để check phiếu thu tiền mặt
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng trong hệ thống.";
                return RedirectToAction(nameof(Deliveries));
            }

            // 3. LOGIC KIỂM TRA MỚI (FIX LỖI 404 TẠI ĐÂY)

            // Nếu đơn đã hoàn thành (Status = 10: Rented) -> Chặn
            if (booking.StatusId == 10)
            {
                TempData["WarningMessage"] = "Đơn hàng này đã giao máy rồi!";
                return RedirectToAction(nameof(Deliveries));
            }

            // Kiểm tra thanh toán: 
            // Hợp lệ nếu: (Status là 12 - Banked) HOẶC (Đã có Phiếu thu trong BookingReceipts)
            bool isPaid = booking.StatusId == 12 || (booking.BookingReceipts != null && booking.BookingReceipts.Any());

            if (!isPaid)
            {
                // [QUAN TRỌNG]: Nếu chưa thanh toán -> Quay về danh sách, KHÔNG redirect sang QuickHandover nữa
                TempData["ErrorMessage"] = "Đơn hàng CHƯA THANH TOÁN. Không thể giao máy!";
                return RedirectToAction(nameof(Deliveries));
            }

            // 4. Chuẩn bị dữ liệu hiển thị (ViewModel)
            var detail = booking.Laptop?.LaptopDetails?.FirstOrDefault();

            var model = new DeliveryViewModel
            {
                BookingId = booking.Id,
                CustomerName = $"{booking.Customer?.LastName} {booking.Customer?.FirstName}",
                LaptopName = booking.Laptop?.Name,
                StartDate = booking.StartTime,
                EndDate = booking.EndTime,
                TotalPrice = booking.TotalPrice,

                // Thông số kỹ thuật
                Cpu = detail?.Cpu,
                RamSize = detail?.RamSize,
                Storage = detail?.Storage,
                Gpu = detail?.Gpu,
                ScreenSize = detail?.ScreenSize,

                // Ảnh và Giấy tờ
                LaptopImageUrl = booking.Laptop?.ImageUrl,
                IdCardUrl = booking.IdNoUrl,
                StudentCardUrl = booking.StudentUrl,

                // Các checkbox mặc định
                ScreenCondition = true,
                KeyboardCondition = true,
                MouseCondition = true,
                ChargerCondition = true,
                BodyCondition = true,
                BatteryLevel = 100,
                IsIdentityVerified = false
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
            // 1. Validate: Kiểm tra checkbox xác nhận danh tính
            if (!model.IsIdentityVerified)
            {
                ModelState.AddModelError("IsIdentityVerified", "Bạn chưa xác nhận thông tin giấy tờ tùy thân của khách!");
            }

            // 2. Nếu có lỗi (Model không hợp lệ HOẶC chưa tick xác nhận)
            if (!ModelState.IsValid)
            {
                // [QUAN TRỌNG] LOAD LẠI DỮ LIỆU HIỂN THỊ
                // Vì form chỉ gửi input, không gửi text hiển thị, nên ta phải query lại DB
                var booking = await _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Laptop).ThenInclude(l => l.LaptopDetails)
                    .FirstOrDefaultAsync(b => b.Id == model.BookingId);

                if (booking != null)
                {
                    // Map lại các thông tin hiển thị cơ bản
                    model.CustomerName = $"{booking.Customer?.LastName} {booking.Customer?.FirstName}";
                    model.LaptopName = booking.Laptop?.Name;
                    model.StartDate = booking.StartTime;
                    model.EndDate = booking.EndTime;
                    model.TotalPrice = booking.TotalPrice;

                    // Map lại ảnh giấy tờ & Ảnh laptop hiện tại (để hiển thị lại nếu user chưa chọn ảnh mới)
                    model.IdCardUrl = booking.IdNoUrl;
                    model.StudentCardUrl = booking.StudentUrl;
                    model.LaptopImageUrl = booking.Laptop?.ImageUrl;

                    // Map lại thông số kỹ thuật
                    var detail = booking.Laptop?.LaptopDetails?.FirstOrDefault();
                    if (detail != null)
                    {
                        model.Cpu = detail.Cpu;
                        model.RamSize = detail.RamSize;
                        model.Storage = detail.Storage;
                        model.Gpu = detail.Gpu;
                        model.ScreenSize = detail.ScreenSize;
                        model.Os = detail.Os;
                    }
                }

                // Trả về View kèm thông báo lỗi và dữ liệu vừa load lại
                return View(model);
            }

            // 3. XỬ LÝ LOGIC GIAO MÁY CHÍNH THỨC
            var bookingToUpdate = await _context.Bookings
                .Include(b => b.BookingReceipts)
                .Include(b => b.Laptop) // Include Laptop để cập nhật ảnh
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (bookingToUpdate == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy booking.";
                return RedirectToAction(nameof(Deliveries));
            }

            // A. Check trạng thái: Nếu đã là Rented (10) thì chặn
            if (bookingToUpdate.StatusId == 10)
            {
                TempData["ErrorMessage"] = "Booking này đã hoàn tất giao máy rồi!";
                return RedirectToAction(nameof(Deliveries));
            }

            // B. Check thanh toán (CẬP NHẬT MỚI): 
            // Hợp lệ nếu: (Đã có phiếu thu tiền mặt) HOẶC (Trạng thái là 12 - Banked/Đã CK Online)
            bool isPaid = (bookingToUpdate.BookingReceipts != null && bookingToUpdate.BookingReceipts.Any())
                          || bookingToUpdate.StatusId == 12;

            if (!isPaid)
            {
                TempData["ErrorMessage"] = "Đơn hàng CHƯA THANH TOÁN. Vui lòng thu tiền trước!";
                return RedirectToAction("QuickHandover", "Staff", new { searchBookingId = bookingToUpdate.Id });
            }

            try
            {
                // C. Xử lý Upload ảnh tình trạng (Lưu vào wwwroot và Update DB)
                if (model.ConditionImage != null && model.ConditionImage.Length > 0)
                {
                    // 1. Tạo tên file duy nhất
                    var fileName = $"delivery_{bookingToUpdate.LaptopId}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{Path.GetExtension(model.ConditionImage.FileName)}";

                    // 2. Định nghĩa thư mục lưu
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "laptops");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }
                    var filePath = Path.Combine(uploadPath, fileName);

                    // 3. Lưu file vật lý
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ConditionImage.CopyToAsync(stream);
                    }

                    // 4. CẬP NHẬT ẢNH MỚI VÀO DATABASE (Bảng Laptop)
                    if (bookingToUpdate.Laptop != null)
                    {
                        bookingToUpdate.Laptop.ImageUrl = "/images/laptops/" + fileName;
                    }
                }

                // D. Cập nhật trạng thái đơn hàng thành 'Rented' (10)
                bookingToUpdate.StatusId = 10;
                bookingToUpdate.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Giao máy thành công đơn #{bookingToUpdate.Id}! Chúc khách hàng trải nghiệm tốt.";
                return RedirectToAction(nameof(Deliveries));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming delivery");
                TempData["ErrorMessage"] = "Lỗi hệ thống khi giao máy. Vui lòng thử lại!";

                // Load lại model để tránh trang trắng (bạn có thể copy đoạn load data ở trên xuống đây nếu cần kỹ hơn)
                return View(model);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLaptopImageOnly(long bookingId, IFormFile? conditionImage)
        {
            // 1. Tìm booking và laptop
            var booking = await _context.Bookings.Include(b => b.Laptop).FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null || booking.Laptop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin máy.";
                return RedirectToAction(nameof(CreateDelivery), new { id = bookingId });
            }

            // 2. Xử lý lưu ảnh (Nếu có chọn file)
            if (conditionImage != null && conditionImage.Length > 0)
            {
                try
                {
                    var fileName = $"laptop_{booking.LaptopId}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(conditionImage.FileName)}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "laptops");

                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await conditionImage.CopyToAsync(stream);
                    }

                    // Cập nhật Database
                    booking.Laptop.ImageUrl = "/images/laptops/" + fileName;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đã cập nhật ảnh mới nhất cho máy!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi upload: " + ex.Message;
                }
            }
            else
            {
                TempData["WarningMessage"] = "Vui lòng chọn ảnh trước khi bấm Cập nhật.";
            }

            // 3. Quay lại trang Giao máy
            return RedirectToAction(nameof(CreateDelivery), new { id = bookingId });
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
        
        // ==========================================
        // HOÀN THÀNH ĐƠN THUÊ (Complete Booking)
        // ==========================================
        
        /// <summary>
        /// Hoàn thành đơn thuê - Chuyển sang trạng thái Close
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteBooking(long bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Laptop)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn thuê.";
                return RedirectToAction(nameof(RentedLaptops));
            }

            // Chỉ cho phép hoàn thành nếu đang ở trạng thái Rented (10)
            if (booking.StatusId != 10)
            {
                TempData["ErrorMessage"] = "Đơn thuê này không ở trạng thái Đang thuê.";
                return RedirectToAction(nameof(RentedLaptops));
            }

            try
            {
                // Chuyển Booking sang trạng thái Close (8)
                booking.StatusId = 8;
                booking.UpdatedDate = DateTime.Now;

                // Chuyển Laptop về trạng thái Available (9) - Sẵn sàng cho thuê lại
                if (booking.Laptop != null)
                {
                    booking.Laptop.StatusId = 9; // Available
                    booking.Laptop.UpdatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã hoàn thành đơn thuê #{bookingId}. Máy đã sẵn sàng trả về cho Student.";
                return RedirectToAction(nameof(DueForReturnLaptops));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hoàn thành đơn thuê");
                TempData["ErrorMessage"] = "Lỗi hệ thống khi hoàn thành đơn thuê.";
                return RedirectToAction(nameof(RentedLaptops));
            }
        }
        
        /// <summary>
        /// Tạo thông báo trả máy về cho Student
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReturnNotification(long bookingId, long laptopId, long studentId, string pickupLocation, DateTime appointmentTime)
        {
            var booking = await _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Student)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn thuê.";
                return RedirectToAction(nameof(DueForReturnLaptops));
            }

            // Kiểm tra trạng thái
            if (booking.StatusId != 8)
            {
                TempData["ErrorMessage"] = "Đơn này chưa hoàn thành, không thể tạo thông báo trả máy.";
                return RedirectToAction(nameof(DueForReturnLaptops));
            }

            try
            {
                // Lưu thông tin lịch hẹn vào RejectReason (sử dụng field có sẵn)
                var notificationInfo = $"RETURN_SCHEDULE|{pickupLocation}|{appointmentTime:yyyy-MM-dd HH:mm}";
                booking.RejectReason = notificationInfo;
                booking.UpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã gửi thông báo trả máy đến Student. Chờ Student xác nhận nhận máy tại {pickupLocation} lúc {appointmentTime:HH:mm dd/MM/yyyy}.";
                return RedirectToAction(nameof(DueForReturnLaptops));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo trả máy");
                TempData["ErrorMessage"] = "Lỗi hệ thống khi tạo thông báo.";
                return RedirectToAction(nameof(DueForReturnLaptops));
            }
        }
        
        // ==========================================
        // QUẢN LÝ TRẢ MÁY VỀ CHO SINH VIÊN (Updated)
        // ==========================================

        /// <summary>
        /// Màn hình 1: Danh sách các máy đã hoàn thành thuê (StatusId = 8 - Close)
        /// </summary>
        public async Task<IActionResult> DueForReturnLaptops(string searchString, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;

            // 1. Query: Lấy các booking đã Close (StatusId = 8)
            var query = _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Student)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Customer)
                .Include(b => b.Status)
                .Where(b => b.StatusId == 8); // Close - Đã hoàn thành thuê

            // 2. Filter tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => 
                    (b.Laptop != null && b.Laptop.Name.Contains(searchString)) ||
                    (b.Laptop != null && b.Laptop.Student != null && b.Laptop.Student.LastName.Contains(searchString)) ||
                    (b.Laptop != null && b.Laptop.Student != null && b.Laptop.Student.FirstName.Contains(searchString)) ||
                    (b.Laptop != null && b.Laptop.Student != null && b.Laptop.Student.Email.Contains(searchString)) ||
                    (b.Customer != null && b.Customer.LastName.Contains(searchString)) ||
                    (b.Customer != null && b.Customer.FirstName.Contains(searchString)));
            }

            // 3. Sắp xếp: Ưu tiên booking mới nhất lên đầu
            query = query.OrderByDescending(b => b.UpdatedDate);

            // 4. Phân trang và trả về View
            int pageSize = 5;
            return View(await PaginatedList<Booking>.CreateAsync(query.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        /// <summary>
        /// Action xử lý Form tạo phiếu hẹn (Submit từ Modal)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReturnSchedule(ReturnLaptopViewModel model)
        {
            // Do các trường hiển thị (LaptopName, StudentName...) trong ViewModel là nullable 
            // và không bắt buộc nhập từ form (vì là readonly/hidden), nên ModelState thường sẽ Valid 
            // nếu AppointmentTime hợp lệ.

            // 1. Kiểm tra ID máy
            var laptop = await _context.Laptops
                .Include(l => l.Student)
                .FirstOrDefaultAsync(l => l.Id == model.LaptopId);

            if (laptop == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin máy!";
                return RedirectToAction(nameof(DueForReturnLaptops));
            }

            try
            {
                // 2. Lấy thông tin sinh viên từ Database (để đảm bảo chính xác nhất)
                string studentName = "Sinh viên";
                string studentEmail = "";

                if (laptop.Student != null)
                {
                    studentName = $"{laptop.Student.LastName} {laptop.Student.FirstName}";
                    studentEmail = laptop.Student.Email;
                }

                // 3. Xử lý Logic "Lưu tạm thời" & Gửi thông báo
                // Theo yêu cầu: Địa điểm cố định, Thời gian lưu tạm để báo cho SV.

                string fixedLocation = "Toà Alpha, L300"; // Cố định
                string timeString = model.AppointmentTime.ToString("HH:mm dd/MM/yyyy");

                // --- (A) Giả lập gửi Email/Notification ---
                // Code gửi email thật sẽ đặt ở đây. Ví dụ:
                // _emailService.Send(studentEmail, "Mời nhận máy", $"Đến {fixedLocation} lúc {timeString}...");

                // --- (B) Hiển thị thông báo thành công cho Staff ---
                TempData["SuccessMessage"] = $"Đã lên lịch trả máy '{laptop.Name}'. " +
                                             $"Nhắn SV {studentName} đến {fixedLocation} lúc {timeString}.";

                // --- (C) Cập nhật trạng thái máy (Tuỳ chọn) ---
                // Ví dụ: Đổi sang trạng thái "Chờ SV đến nhận" để không hiện ở list hết hạn nữa
                // laptop.StatusId = 8; // Ví dụ: 8 là 'Waiting for Pickup'
                // await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo lịch trả máy");
                TempData["ErrorMessage"] = "Lỗi hệ thống khi tạo lịch hẹn.";
            }

            // Quay lại trang danh sách
            return RedirectToAction(nameof(DueForReturnLaptops));
        }
    }

}
