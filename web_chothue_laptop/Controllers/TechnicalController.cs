using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using web_chothue_laptop.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http; // [QUAN TRỌNG] Cần thêm dòng này để dùng IFormFile

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
        // 1. DASHBOARD & DANH SÁCH
        // ============================================================
        public async Task<IActionResult> Index(int? pageNumber, string? searchString, string activeTab = "inspection", DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewBag.ActiveTab = activeTab;
            ViewData["CurrentFilter"] = searchString;

            // Cấu hình số dòng mỗi trang
            int pageSizeInspection = 5;
            int pageSizeRepair = 5;
            int pageSizeReport = 5;
            int pageSizeSupport = 5;

            // --- TAB HỖ TRỢ (SUPPORT) ---
            if (activeTab == "support")
            {
                var supportQuery = _context.TechnicalTickets
                    .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                    .Include(t => t.Booking).ThenInclude(b => b!.Customer)
                    .Include(t => t.Status)
                    .Where(t => t.BookingId != null)
                    .AsQueryable();

                supportQuery = supportQuery.OrderBy(t => t.StatusId == 5).ThenByDescending(t => t.CreatedDate);

                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.Trim();
                    if (long.TryParse(searchString, out long searchId))
                        supportQuery = supportQuery.Where(t => t.Id == searchId);
                    else
                    {
                        var searchLower = searchString.ToLower();
                        supportQuery = supportQuery.Where(t =>
                            (t.Laptop != null && t.Laptop.Name.ToLower().Contains(searchLower)) ||
                            (t.Booking != null && t.Booking.Customer != null &&
                                ((t.Booking.Customer.FirstName + " " + t.Booking.Customer.LastName).ToLower().Contains(searchLower))
                            )
                        );
                    }
                }
                return View(await PaginatedList<TechnicalTicket>.CreateAsync(supportQuery.AsNoTracking(), pageNumber ?? 1, pageSizeSupport));
            }

            // --- TAB BÁO CÁO (REPORT) ---
            if (activeTab == "report")
            {
                var reportQuery = _context.TechnicalTickets
                    .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                    .Include(t => t.Laptop).ThenInclude(l => l.Student)
                    .Include(t => t.Status)
                    .Where(t => t.StatusId == 2 || t.StatusId == 8 || t.StatusId == 3); // ✅ Thêm StatusId = 3 (Rejected by Student)

                if (startDate.HasValue) reportQuery = reportQuery.Where(t => t.CreatedDate >= startDate.Value);
                if (endDate.HasValue)
                {
                    var endDateEndOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    reportQuery = reportQuery.Where(t => t.CreatedDate <= endDateEndOfDay);
                }

                ViewData["TotalCompleted"] = await reportQuery.CountAsync();
                ViewData["ApprovedCount"] = await reportQuery.CountAsync(t => t.StatusId == 2);
                ViewData["ClosedCount"] = await reportQuery.CountAsync(t => t.StatusId == 8);
                ViewData["RejectedCount"] = await reportQuery.CountAsync(t => t.StatusId == 3); // ✅ Đếm ticket bị hủy
                ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
                ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

                if (!string.IsNullOrEmpty(searchString))
                {
                    searchString = searchString.Trim();
                    if (long.TryParse(searchString, out long searchId))
                        reportQuery = reportQuery.Where(t => t.Id == searchId);
                    else
                        reportQuery = reportQuery.Where(t => t.Laptop.Name.Contains(searchString) || (t.Laptop.Student != null && (t.Laptop.Student.FirstName + " " + t.Laptop.Student.LastName).Contains(searchString)));
                }

                var pagedReport = reportQuery.OrderByDescending(t => t.UpdatedDate ?? t.CreatedDate);
                return View(await PaginatedList<TechnicalTicket>.CreateAsync(pagedReport.AsNoTracking(), pageNumber ?? 1, pageSizeReport));
            }

            // --- QUERY CHUNG CHO INSPECTION & REPAIR ---
            // ✅ Loại bỏ ticket đã hoàn thành (2, 8) VÀ ticket bị Student hủy (3)
            var activeTickets = _context.TechnicalTickets.Where(t => 
                t.StatusId != 2 && 
                t.StatusId != 8 && 
                t.StatusId != 3 && 
                t.BookingId == null);

            ViewData["TotalCount"] = await activeTickets.CountAsync();
            ViewData["ProcessingCount"] = await activeTickets.CountAsync(t => t.StatusId == 4);
            ViewData["FixedCount"] = await activeTickets.CountAsync(t => t.StatusId == 5);
            ViewData["BrokenCount"] = (int)ViewData["TotalCount"] - (int)ViewData["ProcessingCount"] - (int)ViewData["FixedCount"] - await activeTickets.CountAsync(t => t.StatusId == 1);

            // --- TAB YÊU CẦU KIỂM TRA (INSPECTION) ---
            var inspectionQuery = activeTickets
                .Include(t => t.Laptop).ThenInclude(l => l.Student)
                .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                .Include(t => t.Laptop).ThenInclude(l => l.LaptopDetails)
                .Where(t => t.StatusId == 1);

            if (activeTab == "inspection" && !string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();
                if (long.TryParse(searchString, out long searchId))
                    inspectionQuery = inspectionQuery.Where(t => t.Id == searchId);
                else
                    inspectionQuery = inspectionQuery.Where(t => (t.Laptop != null && t.Laptop.Name.ToLower().Contains(searchString)) || (t.Laptop.Student != null && (t.Laptop.Student.FirstName + " " + t.Laptop.Student.LastName).ToLower().Contains(searchString)));
            }

            inspectionQuery = inspectionQuery.OrderBy(t => t.CreatedDate);
            ViewBag.InspectionTotalCount = await inspectionQuery.CountAsync();
            int inspectionPage = (activeTab == "inspection") ? (pageNumber ?? 1) : 1;
            ViewBag.InspectionList = await PaginatedList<TechnicalTicket>.CreateAsync(inspectionQuery.AsNoTracking(), inspectionPage, pageSizeInspection);

            // --- TAB SỬA CHỮA (REPAIR) ---
            var listRepair = activeTickets.Include(t => t.Laptop).Include(t => t.Status).Where(t => t.StatusId != 1).AsQueryable();
            if (activeTab == "repair" && !string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                if (long.TryParse(searchString, out long searchId)) listRepair = listRepair.Where(t => t.Id == searchId);
                else listRepair = listRepair.Where(t => t.Laptop != null && t.Laptop.Name.Contains(searchString));
            }
            listRepair = listRepair.OrderByDescending(t => t.StatusId == 11 || t.StatusId == 3).ThenByDescending(t => t.StatusId == 4).ThenByDescending(t => t.UpdatedDate);
            int repairPage = (activeTab == "repair") ? (pageNumber ?? 1) : 1;
            return View(await PaginatedList<TechnicalTicket>.CreateAsync(listRepair.AsNoTracking(), repairPage, pageSizeRepair));
        }

        // ============================================================
        // 2. [MỚI] XỬ LÝ CẬP NHẬT THÔNG TIN KIỂM TRA (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInspectionInfo(long ticketId, string description, IFormFile? inspectionImage)
        {
            // Load ticket và kèm theo dữ liệu Laptop để update
            var ticket = await _context.TechnicalTickets
                .Include(t => t.Laptop)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ticket.";
                return RedirectToAction(nameof(Index), new { activeTab = "inspection" });
            }

            // 1. Cập nhật Mô tả / Tình trạng
            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["ErrorMessage"] = "Mô tả tình trạng không được để trống.";
                return RedirectToAction(nameof(InspectionDetails), new { id = ticketId });
            }
            ticket.Description = description;

            // 2. Xử lý Upload ảnh (Lưu vào wwwroot và update DB)
            if (inspectionImage != null && inspectionImage.Length > 0)
            {
                try
                {
                    // Kiểm tra Laptop có tồn tại không
                    if (ticket.Laptop != null)
                    {
                        // Tạo tên file duy nhất để tránh trùng lặp (dùng GUID hoặc TimeStamp)
                        var fileName = $"laptop_{ticket.Laptop.Id}_{Guid.NewGuid()}{Path.GetExtension(inspectionImage.FileName)}";

                        // Định nghĩa thư mục lưu trữ (Ví dụ: wwwroot/images/laptops)
                        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "laptops");

                        // Tạo thư mục nếu chưa tồn tại
                        if (!Directory.Exists(uploadFolder))
                        {
                            Directory.CreateDirectory(uploadFolder);
                        }

                        var filePath = Path.Combine(uploadFolder, fileName);

                        // Lưu file vật lý vào server
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await inspectionImage.CopyToAsync(stream);
                        }

                        // CẬP NHẬT ĐƯỜNG DẪN VÀO DATABASE (Cột IMAGE_URL)
                        // Lưu đường dẫn tương đối để web có thể hiển thị
                        ticket.Laptop.ImageUrl = "/images/laptops/" + fileName;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi upload ảnh: " + ex.Message;
                    return RedirectToAction(nameof(InspectionDetails), new { id = ticketId });
                }
            }

            ticket.UpdatedDate = DateTime.Now;

            // Lưu thay đổi vào Database (bao gồm cả Description và ImageUrl mới)
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật thông tin kiểm tra và hình ảnh.";

            // Quay lại trang chi tiết
            return RedirectToAction(nameof(InspectionDetails), new { id = ticketId });
        }

        // ============================================================
        // 3. CÁC ACTION KHÁC (DUYỆT, TỪ CHỐI, SỬA CHỮA...)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLaptop(long ticketId)
        {
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null)
            {
                // 1. Cập nhật ticket sang Approved
                ticket.StatusId = 2; // Approved
                ticket.TechnicalResponse = "Đã đạt chuẩn. Chuyển sang Staff để duyệt lên web.";
                ticket.UpdatedDate = DateTime.Now;
                
                // 2. GIỮ NGUYÊN Laptop.StatusId = 1 (Pending) - Chờ Staff duyệt
                // KHÔNG tự động chuyển sang Approved
                // if (ticket.Laptop != null) ticket.Laptop.StatusId = 2; // ❌ XÓA DÒNG NÀY
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã duyệt! Thiết bị đã được chuyển sang Staff để duyệt lên web.";
            }
            return RedirectToAction(nameof(Index), new { activeTab = "inspection" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLaptop(long ticketId, string reason, string rejectType)
        {
            var ticket = await _context.TechnicalTickets
                .Include(t => t.Laptop)
                    .ThenInclude(l => l.Student)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
                
            if (ticket != null && ticket.Laptop != null)
            {
                if (rejectType == "fixing")
                {
                    // Trường hợp: Cần sửa chữa (Fixing)
                    // KHÔNG ĐỔI Ticket.StatusId - Giữ nguyên = 1 (Pending)
                    // ticket.StatusId = 3; // ❌ XÓA DÒNG NÀY
                    
                    ticket.TechnicalResponse = "CẦN SỬA CHỮA: " + reason;
                    ticket.UpdatedDate = DateTime.Now;
                    
                    // Chỉ đổi Laptop.StatusId = 4 (Fixing)
                    ticket.Laptop.StatusId = 4; // Fixing
                    ticket.Laptop.RejectReason = "Cần sửa chữa: " + reason;
                    ticket.Laptop.UpdatedDate = DateTime.Now;
                    
                    TempData["WarningMessage"] = "Đã gửi yêu cầu sửa chữa cho Student. Chờ Student đồng ý.";
                }
                else
                {
                    // Trường hợp: Từ chối hoàn toàn (Rejected)
                    ticket.StatusId = 3; // Rejected
                    ticket.TechnicalResponse = "TỪ CHỐI: " + reason;
                    ticket.UpdatedDate = DateTime.Now;
                    
                    ticket.Laptop.StatusId = 3; // Rejected
                    ticket.Laptop.RejectReason = reason;
                    ticket.Laptop.UpdatedDate = DateTime.Now;
                    
                    TempData["WarningMessage"] = "Đã từ chối thiết bị hoàn toàn. Lý do đã được gửi cho Student.";
                }
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { activeTab = "inspection" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomerTicket(long ticketId, int statusId, string technicalResponse)
        {
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return RedirectToAction(nameof(Index), new { activeTab = "support" });

            ticket.StatusId = statusId;
            ticket.UpdatedDate = DateTime.Now;
            if (!string.IsNullOrEmpty(technicalResponse)) ticket.TechnicalResponse = technicalResponse;
            else
            {
                switch (statusId)
                {
                    case 2: ticket.TechnicalResponse = "Kỹ thuật viên đã tiếp nhận yêu cầu."; break;
                    case 4: ticket.TechnicalResponse = "Đang tiến hành kiểm tra và sửa chữa."; break;
                    case 5: ticket.TechnicalResponse = "Đã hoàn thành sửa chữa. Vui lòng kiểm tra."; break;
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = statusId == 5 ? "Đã hoàn thành sửa chữa!" : "Đã cập nhật trạng thái ticket.";
            return RedirectToAction(nameof(Index), new { activeTab = "support" });
        }

        public async Task<IActionResult> InspectionDetails(long id)
        {
            var ticket = await _context.TechnicalTickets
                .Include(t => t.Laptop).ThenInclude(l => l.Brand)
                .Include(t => t.Laptop).ThenInclude(l => l.Student)
                .Include(t => t.Laptop).ThenInclude(l => l.LaptopDetails)
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();
            return View(ticket);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(long? id, string? activeTab, string? searchString)
        {
            if (id == null) return NotFound();
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null) return NotFound();

            // Lưu trạng thái quay về
            ViewBag.ReturnTab = activeTab ?? "repair";
            ViewBag.ReturnSearch = searchString;

            return View(ticket);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, int statusId, string technicalResponse, bool qcWifi, bool qcKeyboard, bool qcScreen, bool qcClean, string? partName, decimal? partPrice)
        {
            var ticket = await _context.TechnicalTickets.Include(t => t.Laptop).FirstOrDefaultAsync(t => t.Id == id);
            if (ticket == null) return NotFound();

            if (statusId == 5) // Fixed - Sửa xong
            {
                if (!qcWifi || !qcKeyboard || !qcScreen || !qcClean) 
                { 
                    TempData["ErrorMessage"] = "Chưa hoàn thành QC!"; 
                    return View(ticket); 
                }
                
                technicalResponse += "\n[SYSTEM]: Sửa xong & QC Passed.";
                
                // Kiểm tra laptop có đang ở trạng thái Fixing không
                if (ticket.Laptop != null && ticket.Laptop.StatusId == 4)
                {
                    // Nếu đang Fixing → Chuyển sang Available (9)
                    ticket.Laptop.StatusId = 9; // Available - Đưa lên web
                    ticket.Laptop.RejectReason = null; // Xóa lý do từ chối
                    technicalResponse += " Laptop đã được chuyển sang Available.";
                }
                else
                {
                    // Nếu không phải Fixing → Chuyển về Pending để duyệt lại
                    statusId = 1;
                    if (ticket.Laptop != null) ticket.Laptop.StatusId = 1;
                    technicalResponse += " Chuyển về Pending để duyệt.";
                }
            }
            else if (statusId == 6) // Need parts
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
                if (statusId == 1) ticket.Laptop.StatusId = 1;
                else if (statusId == 4) ticket.Laptop.StatusId = 4;
                else if (statusId == 3 || statusId == 11) ticket.Laptop.StatusId = 11;
            }

            await _context.SaveChangesAsync();
            
            if (statusId == 1)
            {
                TempData["SuccessMessage"] = "Đã sửa xong! Thiết bị quay lại mục Yêu Cầu Kiểm Tra.";
                return RedirectToAction(nameof(Index), new { activeTab = "inspection" });
            }
            else if (ticket.Laptop?.StatusId == 9)
            {
                TempData["SuccessMessage"] = "Đã sửa xong! Laptop đã được niêm yết lên web.";
                return RedirectToAction(nameof(Index), new { activeTab = "repair" });
            }
            
            return RedirectToAction(nameof(Index), new { activeTab = "repair" });
        }
    }
}