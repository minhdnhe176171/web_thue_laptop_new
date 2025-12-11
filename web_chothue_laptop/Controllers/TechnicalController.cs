using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Controllers
{
    public class TechnicalController : Controller
    {
        private readonly Swp391LaptopContext _context;

        public TechnicalController(Swp391LaptopContext context)
        {
            _context = context;
        }

        // ============================================================
        // 1. DASHBOARD & DANH SÁCH
        // ============================================================
        public async Task<IActionResult> Index(int? pageNumber, string? searchString, string activeTab = "inspection")
        {
            // --- BƯỚC 1: LỌC DỮ LIỆU CƠ BẢN ---
            // Chỉ lấy các Ticket chưa hoàn thành (Loại bỏ Approved - ID 2 và Close - ID 8 nếu có)
            // Điều này giúp Ticket biến mất khỏi Dashboard khi đã duyệt.
            var activeTickets = _context.TechnicalTickets.Where(t => t.StatusId != 2 && t.StatusId != 8);

            // --- BƯỚC 2: TÍNH TOÁN SỐ LIỆU (Dựa trên activeTickets đã lọc) ---
            int totalCount = await activeTickets.CountAsync(); // Tổng số phiếu (Đã trừ cái vừa duyệt)
            int processingCount = await activeTickets.CountAsync(t => t.StatusId == 4); // Đang sửa
            int fixedCount = await activeTickets.CountAsync(t => t.StatusId == 5);      // Đã sửa xong
            int pendingCount = await activeTickets.CountAsync(t => t.StatusId == 1);    // Chờ duyệt

            // Máy hỏng = Tổng - (Các trạng thái kia). 
            // Vì "Approved" đã bị lọc khỏi 'totalCount' ngay từ đầu, nên số liệu sẽ giảm chuẩn xác.
            int brokenCount = totalCount - processingCount - fixedCount - pendingCount;

            ViewData["TotalCount"] = totalCount;
            ViewData["ProcessingCount"] = processingCount;
            ViewData["FixedCount"] = fixedCount;
            ViewData["BrokenCount"] = brokenCount;
            ViewBag.ActiveTab = activeTab;

            // --- BƯỚC 3: LẤY DỮ LIỆU TAB 1 (YÊU CẦU KIỂM TRA) ---
            ViewBag.InspectionList = await activeTickets
                .Include(t => t.Laptop).ThenInclude(l => l.Student)
                .Where(t => t.StatusId == 1)
                .OrderBy(t => t.CreatedDate)
                .ToListAsync();

            // --- BƯỚC 4: LẤY DỮ LIỆU TAB 2 (DANH SÁCH SỬA CHỮA) ---
            // Lấy tất cả activeTickets ngoại trừ Pending (ID 1)
            // Vì activeTickets đã loại ID 2 rồi, nên đơn Approved sẽ KHÔNG hiện ở đây.
            var listRepair = activeTickets
                .Include(t => t.Laptop)
                .Include(t => t.Status)
                .Where(t => t.StatusId != 1)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                if (long.TryParse(searchString, out long searchId))
                {
                    listRepair = listRepair.Where(t => t.Id == searchId);
                }
                else
                {
                    listRepair = listRepair.Where(t => t.Laptop != null && t.Laptop.Name.Contains(searchString));
                }
            }

            listRepair = listRepair.OrderByDescending(t => t.StatusId == 11 || t.StatusId == 3)
                                   .ThenByDescending(t => t.StatusId == 4)
                                   .ThenByDescending(t => t.UpdatedDate);

            ViewData["CurrentFilter"] = searchString;
            int pageSize = 5;
            return View(await PaginatedList<TechnicalTicket>.CreateAsync(listRepair.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // ============================================================
        // 2. ACTION DUYỆT (APPROVE) - ĐẨY SANG STAFF
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLaptop(long ticketId)
        {
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null)
            {
                // 1. Ticket -> Approved (2)
                // Khi status là 2, Query ở hàm Index sẽ lọc nó ra -> BIẾN MẤT KHỎI TECHNICAL
                ticket.StatusId = 2;
                ticket.TechnicalResponse = "Đã đạt chuẩn. Chuyển sang Staff.";
                ticket.UpdatedDate = DateTime.Now;

                // 2. Laptop -> Approved (2)
                // Để Staff nhìn thấy và cho thuê
                if (ticket.Laptop != null)
                {
                    ticket.Laptop.StatusId = 2;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã duyệt! Thiết bị đã được chuyển sang Staff để cho thuê.";
            }
            // Quay lại dashboard (Lúc này ticket vừa duyệt sẽ biến mất)
            return RedirectToAction(nameof(Index), new { activeTab = "inspection" });
        }

        // ============================================================
        // 3. CÁC HÀM KHÁC (GIỮ NGUYÊN LOGIC CŨ CỦA BẠN)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLaptop(long ticketId, string reason)
        {
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.StatusId = 3; // Rejected (Vẫn hiện ở Dashboard nhưng ở Tab sửa chữa)
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

                // QUAN TRỌNG: Sửa xong -> Về Pending (1) để hiện lại Tab 1 cho Technical duyệt lần cuối
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

        // ============================================================
        // 4. BÁO CÁO - LỊCH SỬ SỬA CHỮA
        // ============================================================
        public async Task<IActionResult> Report(int? pageNumber, string? searchString, DateTime? startDate, DateTime? endDate)
        {
            // Lấy tất cả các ticket đã hoàn thành (Approved - ID 2 hoặc Closed - ID 8)
            var completedTickets = _context.TechnicalTickets
                .Include(t => t.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(t => t.Laptop)
                    .ThenInclude(l => l.Student)
                .Include(t => t.Status)
                .Include(t => t.Technical)
                .Include(t => t.Staff)
                .Where(t => t.StatusId == 2 || t.StatusId == 8) // Chỉ lấy các ticket đã hoàn thành
                .AsQueryable();

            // Lọc theo ngày bắt đầu
            if (startDate.HasValue)
            {
                completedTickets = completedTickets.Where(t => t.CreatedDate >= startDate.Value);
            }

            // Lọc theo ngày kết thúc
            if (endDate.HasValue)
            {
                var endDateEndOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                completedTickets = completedTickets.Where(t => t.CreatedDate <= endDateEndOfDay);
            }

            // Tìm kiếm theo mã ticket
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                if (long.TryParse(searchString, out long searchId))
                {
                    completedTickets = completedTickets.Where(t => t.Id == searchId);
                }
            }

            // Sắp xếp theo ngày cập nhật mới nhất
            completedTickets = completedTickets.OrderByDescending(t => t.UpdatedDate ?? t.CreatedDate);

            // Thống kê
            var totalCompleted = await completedTickets.CountAsync();
            var approvedCount = await completedTickets.CountAsync(t => t.StatusId == 2);
            var closedCount = await completedTickets.CountAsync(t => t.StatusId == 8);

            ViewData["TotalCompleted"] = totalCompleted;
            ViewData["ApprovedCount"] = approvedCount;
            ViewData["ClosedCount"] = closedCount;
            ViewData["CurrentFilter"] = searchString;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

            // Phân trang
            int pageSize = 10;
            return View(await PaginatedList<TechnicalTicket>.CreateAsync(completedTickets.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
    }
}