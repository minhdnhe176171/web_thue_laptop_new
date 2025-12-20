using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using web_chothue_laptop.Models;
using Microsoft.AspNetCore.Authorization;

namespace web_chothue_laptop.Controllers
{
    [Authorize(Roles = "Technical")]
    public class TechnicalController : Controller
    {
        private readonly Swp391LaptopContext _context;

        public TechnicalController(Swp391LaptopContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. DASHBOARD & DANH SÁCH (ĐÃ TỐI ƯU PHÂN TRANG RIÊNG BIỆT)
        // ============================================================
        public async Task<IActionResult> Index(int? pageNumber, string? searchString, string activeTab = "inspection", DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewBag.ActiveTab = activeTab;

            // CẤU HÌNH SỐ LƯỢNG DÒNG MỖI TRANG
            int pageSizeInspection = 5;
            int pageSizeRepair = 5;
            int pageSizeReport = 5;
            int pageSizeSupport = 5; 

            // ------------------------------------------------------------
            // [MỚI] TRƯỜNG HỢP: TAB HỖ TRỢ KHÁCH HÀNG (SUPPORT)
            // Logic: Lấy Ticket có BookingId != null (Tức là từ Customer)
            // ------------------------------------------------------------
            if (activeTab == "support")
            {
                var supportQuery = _context.TechnicalTickets
                    .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                    .Include(t => t.Booking).ThenInclude(b => b!.Customer) // Include Customer để hiển thị tên khách
                    .Include(t => t.Status)
                    .Where(t => t.BookingId != null) // QUAN TRỌNG: Chỉ lấy ticket có liên kết với Booking (của khách)
                    .AsQueryable();

                // Sắp xếp: Ưu tiên Active (Mới, Đã nhận, Đang sửa) lên trước -> Đã xong xuống dưới
                // Sau đó sắp xếp theo ngày tạo mới nhất
                supportQuery = supportQuery.OrderBy(t => t.StatusId == 5) // Status 5 (Fixed) sẽ nằm dưới
                                           .ThenByDescending(t => t.CreatedDate);

                // Logic tìm kiếm cho tab Support
                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.Trim();

                    // Case 1: Search by ID (Nếu là số thì tìm theo ID)
                    if (long.TryParse(searchString, out long searchId))
                    {
                        supportQuery = supportQuery.Where(t => t.Id == searchId);
                    }
                    // Case 2: Search by String (Tên Laptop hoặc Tên Khách)
                    else
                    {
                        // Normalize to lowercase for better matching
                        var searchLower = searchString.ToLower();

                        supportQuery = supportQuery.Where(t =>
                            // 1. Search Laptop Name
                            (t.Laptop != null && t.Laptop.Name.ToLower().Contains(searchLower))
                            ||
                            // 2. Search Customer Name
                            (t.Booking != null && t.Booking.Customer != null &&
                                (
                                    t.Booking.Customer.FirstName.ToLower().Contains(searchLower) ||
                                    t.Booking.Customer.LastName.ToLower().Contains(searchLower) ||
                                    // Allow searching full name (e.g., "Nguyen Van")
                                    (t.Booking.Customer.FirstName + " " + t.Booking.Customer.LastName).ToLower().Contains(searchLower)
                                )
                            )
                        );
                    }
                }

                // Trả về View cho tab Support
                return View(await PaginatedList<TechnicalTicket>.CreateAsync(supportQuery.AsNoTracking(), pageNumber ?? 1, pageSizeSupport));
            }

            // ------------------------------------------------------------
            // TRƯỜNG HỢP: TAB BÁO CÁO (REPORT)
            // ------------------------------------------------------------
            if (activeTab == "report")
            {
                var reportQuery = _context.TechnicalTickets
                    .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                    .Include(t => t.Laptop).ThenInclude(l => l.Student)
                    .Include(t => t.Status)
                    // Lấy Approved(2) của luồng nhập kho hoặc Closed(8)
                    // Hoặc lấy Fixed(5) của luồng support nếu muốn báo cáo chung
                    .Where(t => t.StatusId == 2 || t.StatusId == 8);

                // Lọc theo ngày
                if (startDate.HasValue) reportQuery = reportQuery.Where(t => t.CreatedDate >= startDate.Value);
                if (endDate.HasValue)
                {
                    var endDateEndOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    reportQuery = reportQuery.Where(t => t.CreatedDate <= endDateEndOfDay);
                }

                // TÍNH TOÁN SỐ LIỆU THỐNG KÊ
                ViewData["TotalCompleted"] = await reportQuery.CountAsync();
                ViewData["ApprovedCount"] = await reportQuery.CountAsync(t => t.StatusId == 2);
                ViewData["ClosedCount"] = await reportQuery.CountAsync(t => t.StatusId == 8);

                // Lưu dữ liệu filter
                ViewData["CurrentFilter"] = searchString;
                ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
                ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

                // Tìm kiếm Text
                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.Trim();
                    if (long.TryParse(searchString, out long searchId))
                        reportQuery = reportQuery.Where(t => t.Id == searchId);
                    else
                        reportQuery = reportQuery.Where(t => t.Laptop.Name.Contains(searchString)
                            || (t.Laptop.Student != null && (t.Laptop.Student.FirstName + " " + t.Laptop.Student.LastName).Contains(searchString)));
                }

                var pagedReport = reportQuery.OrderByDescending(t => t.UpdatedDate ?? t.CreatedDate);
                return View(await PaginatedList<TechnicalTicket>.CreateAsync(pagedReport.AsNoTracking(), pageNumber ?? 1, pageSizeReport));
            }

            // ------------------------------------------------------------
            // TRƯỜNG HỢP: TAB KIỂM TRA & SỬA CHỮA (MẶC ĐỊNH - KHO)
            // Lọc BookingId == null để không bị lẫn ticket của khách hàng
            // ------------------------------------------------------------

            // 1. Query cơ bản (Chưa hoàn thành, thuộc kho)
            var activeTickets = _context.TechnicalTickets.Where(t => t.StatusId != 2 && t.StatusId != 8 && t.BookingId == null);

            // 2. Tính toán thống kê Dashboard (Chỉ tính cho kho)
            int totalCount = await activeTickets.CountAsync();
            int processingCount = await activeTickets.CountAsync(t => t.StatusId == 4);
            int fixedCount = await activeTickets.CountAsync(t => t.StatusId == 5);
            int pendingCount = await activeTickets.CountAsync(t => t.StatusId == 1);
            int brokenCount = totalCount - processingCount - fixedCount - pendingCount;

            ViewData["TotalCount"] = totalCount;
            ViewData["ProcessingCount"] = processingCount;
            ViewData["FixedCount"] = fixedCount;
            ViewData["BrokenCount"] = brokenCount;
            ViewData["CurrentFilter"] = searchString;

            // 3. XỬ LÝ TAB 1: YÊU CẦU KIỂM TRA (INSPECTION)
            var inspectionQuery = activeTickets
                .Include(t => t.Laptop).ThenInclude(l => l.Student)
                .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                .Include(t => t.Laptop).ThenInclude(l => l.LaptopDetails)
                .Where(t => t.StatusId == 1)
                .OrderBy(t => t.CreatedDate);

            ViewBag.InspectionTotalCount = await inspectionQuery.CountAsync();

            int inspectionPage = (activeTab == "inspection") ? (pageNumber ?? 1) : 1;
            ViewBag.InspectionList = await PaginatedList<TechnicalTicket>.CreateAsync(inspectionQuery.AsNoTracking(), inspectionPage, pageSizeInspection);


            // 4. XỬ LÝ TAB 2: DANH SÁCH SỬA CHỮA (REPAIR) - KHO
            var listRepair = activeTickets
                .Include(t => t.Laptop)
                .Include(t => t.Status)
                .Where(t => t.StatusId != 1)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                if (long.TryParse(searchString, out long searchId))
                    listRepair = listRepair.Where(t => t.Id == searchId);
                else
                    listRepair = listRepair.Where(t => t.Laptop != null && t.Laptop.Name.Contains(searchString));
            }

            listRepair = listRepair.OrderByDescending(t => t.StatusId == 11 || t.StatusId == 3)
                                   .ThenByDescending(t => t.StatusId == 4)
                                   .ThenByDescending(t => t.UpdatedDate);

            int repairPage = (activeTab == "repair") ? (pageNumber ?? 1) : 1;

            return View(await PaginatedList<TechnicalTicket>.CreateAsync(listRepair.AsNoTracking(), repairPage, pageSizeRepair));
        }

        // ============================================================
        // [MỚI] 2. ACTION: CẬP NHẬT TRẠNG THÁI TICKET KHÁCH HÀNG
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomerTicket(long ticketId, int statusId, string technicalResponse)
        {
            var ticket = await _context.TechnicalTickets
                .Include(t => t.Laptop)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ticket.";
                return RedirectToAction(nameof(Index), new { activeTab = "support" });
            }

            // 1. Cập nhật trạng thái
            ticket.StatusId = statusId;
            ticket.UpdatedDate = DateTime.Now;

            // 2. Cập nhật phản hồi (Ghi chú tiến độ)
            if (!string.IsNullOrEmpty(technicalResponse))
            {
                ticket.TechnicalResponse = technicalResponse;
            }
            else
            {
                // Tạo câu phản hồi mặc định nếu kỹ thuật không nhập gì
                switch (statusId)
                {
                    case 2: ticket.TechnicalResponse = "Kỹ thuật viên đã tiếp nhận yêu cầu."; break;
                    case 4: ticket.TechnicalResponse = "Đang tiến hành kiểm tra và sửa chữa."; break;
                    case 5: ticket.TechnicalResponse = "Đã hoàn thành sửa chữa. Vui lòng kiểm tra."; break;
                }
            }

            await _context.SaveChangesAsync();

            string msg = statusId == 5 ? "Đã hoàn thành sửa chữa!" : "Đã cập nhật trạng thái ticket.";
            TempData["SuccessMessage"] = msg;

            // Quay lại đúng tab Support
            return RedirectToAction(nameof(Index), new { activeTab = "support" });
        }

        // ============================================================
        // 3. ACTION DUYỆT (APPROVE) - ĐẨY SANG STAFF (LUỒNG KHO)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLaptop(long ticketId)
        {
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null)
            {
                // Ticket -> Approved (2) -> Biến mất khỏi Technical
                ticket.StatusId = 2;
                ticket.TechnicalResponse = "Đã đạt chuẩn. Chuyển sang Staff.";
                ticket.UpdatedDate = DateTime.Now;

                // Laptop -> Approved (2) -> Staff thấy để cho thuê
                if (ticket.Laptop != null)
                {
                    ticket.Laptop.StatusId = 2;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã duyệt! Thiết bị đã được chuyển sang Staff để cho thuê.";
            }
            return RedirectToAction(nameof(Index), new { activeTab = "inspection" });
        }

        public async Task<IActionResult> InspectionDetails(long id)
        {
            var ticket = await _context.TechnicalTickets
                .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                .Include(t => t.Laptop).ThenInclude(l => l.Student)
                .Include(t => t.Laptop).ThenInclude(l => l.LaptopDetails)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // ============================================================
        // 4. CÁC HÀM KHÁC (EDIT, REJECT, REPORT)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLaptop(long ticketId, string reason)
        {
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.StatusId = 3; // Rejected
                ticket.TechnicalResponse = "TỪ CHỐI: " + reason;
                ticket.UpdatedDate = DateTime.Now;
                if (ticket.Laptop != null) ticket.Laptop.StatusId = 3;

                await _context.SaveChangesAsync();
                TempData["WarningMessage"] = "Đã từ chối thiết bị.";
            }
            return RedirectToAction(nameof(Index), new { activeTab = "inspection" });
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null) return NotFound();
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, int statusId, string technicalResponse,
            bool qcWifi, bool qcKeyboard, bool qcScreen, bool qcClean,
            string? partName, decimal? partPrice)
        {
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();

            // Logic QC...
            if (statusId == 5) // Fixed
            {
                if (!qcWifi || !qcKeyboard || !qcScreen || !qcClean)
                {
                    TempData["ErrorMessage"] = "Chưa hoàn thành QC!";
                    return View(ticket);
                }
                technicalResponse += "\n[SYSTEM]: Sửa xong & QC Passed. Chuyển về Pending để duyệt.";
                // Sửa xong -> Về Pending (1) để hiện lại Tab 1 cho Technical duyệt lần cuối
                statusId = 1;
            }
            // Logic Linh kiện...
            else if (statusId == 6)
            {
                if (string.IsNullOrEmpty(partName)) { TempData["ErrorMessage"] = "Nhập tên linh kiện."; return View(ticket); }
                technicalResponse += $"\n[REQUEST]: Cần: {partName}";
                statusId = 4;
            }

            ticket.StatusId = statusId;
            ticket.TechnicalResponse = technicalResponse;
            ticket.UpdatedDate = DateTime.Now;

            if (ticket.Laptop != null)
            {
                if (statusId == 1) ticket.Laptop.StatusId = 1; // Về Pending
                else if (statusId == 4) ticket.Laptop.StatusId = 4;
                else if (statusId == 3 || statusId == 11) ticket.Laptop.StatusId = 11;
            }

            await _context.SaveChangesAsync();

            if (statusId == 1)
            {
                TempData["SuccessMessage"] = "Đã sửa xong! Thiết bị quay lại mục Yêu Cầu Kiểm Tra.";
                return RedirectToAction(nameof(Index), new { activeTab = "inspection" });
            }

            return RedirectToAction(nameof(Index), new { activeTab = "repair" });
        }

        // Hàm Report này dùng cho trang Report riêng (nếu có), logic tương tự tab Report ở Index
        public async Task<IActionResult> Report(int? pageNumber, string? searchString, DateTime? startDate, DateTime? endDate)
        {
            var completedTickets = _context.TechnicalTickets
                .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                .Include(t => t.Laptop).ThenInclude(l => l.Student)
                .Include(t => t.Status)
                .Include(t => t.Technical)
                .Include(t => t.Staff)
                .Where(t => t.StatusId == 2 || t.StatusId == 8)
                .AsQueryable();

            if (startDate.HasValue) completedTickets = completedTickets.Where(t => t.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
            {
                var endDateEndOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                completedTickets = completedTickets.Where(t => t.CreatedDate <= endDateEndOfDay);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                if (long.TryParse(searchString, out long searchId)) completedTickets = completedTickets.Where(t => t.Id == searchId);
            }

            completedTickets = completedTickets.OrderByDescending(t => t.UpdatedDate ?? t.CreatedDate);

            ViewData["TotalCompleted"] = await completedTickets.CountAsync();
            ViewData["ApprovedCount"] = await completedTickets.CountAsync(t => t.StatusId == 2);
            ViewData["ClosedCount"] = await completedTickets.CountAsync(t => t.StatusId == 8);
            ViewData["CurrentFilter"] = searchString;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            int pageSize = 10;
            return View(await PaginatedList<TechnicalTicket>.CreateAsync(completedTickets.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
    }
}