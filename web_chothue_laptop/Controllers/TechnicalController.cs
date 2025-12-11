using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
        // 1. DASHBOARD & DANH SÁCH (Có thống kê số liệu)
        // ============================================================
        // ============================================================
        // 1. DASHBOARD & DANH SÁCH (Có thống kê số liệu)
        // ============================================================
        public async Task<IActionResult> Index(int? pageNumber, string? searchString)
        {
            // --- THỐNG KÊ DASHBOARD ---

            // BƯỚC 1: Lấy số liệu các trạng thái đã biết rõ
            var countProcessing = await _context.TechnicalTickets.CountAsync(t => t.StatusId == 4); // Đang sửa
            var countFixed = await _context.TechnicalTickets.CountAsync(t => t.StatusId == 5);      // Đã xong
            var totalCount = await _context.TechnicalTickets.CountAsync();                          // Tổng

            // BƯỚC 2: Tính số lượng "Hư hỏng" bằng phương pháp loại trừ (Total - Đang sửa - Đã xong)
            // Cách này đảm bảo số liệu trên Dashboard luôn khớp tổng 100%, bất kể ID hư hỏng là 1, 11 hay null.
            ViewData["BrokenCount"] = totalCount - countProcessing - countFixed;

            ViewData["ProcessingCount"] = countProcessing;
            ViewData["FixedCount"] = countFixed;
            ViewData["TotalCount"] = totalCount;

            // Lưu từ khóa tìm kiếm
            ViewData["CurrentFilter"] = searchString;

            // Truy vấn dữ liệu cơ bản
            var tickets = _context.TechnicalTickets
                .Include(t => t.Laptop)
                .Include(t => t.Status)
                .Include(t => t.Technical)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                tickets = tickets.Where(t => t.Id.ToString().Contains(searchString!));
            }

            // --- SỬA LẠI LOGIC SẮP XẾP ---
            // Ưu tiên đưa các đơn KHÔNG PHẢI là "Đang sửa" (4) và "Đã xong" (5) lên đầu
            // Nghĩa là các đơn Hư hỏng/Mới (ID 1, 11...) sẽ luôn nằm trên cùng.
            tickets = tickets.OrderByDescending(t => t.StatusId != 4 && t.StatusId != 5)
                             .ThenByDescending(t => t.CreatedDate);

            int pageSize = 4;
            return View(await PaginatedList<TechnicalTicket>.CreateAsync(tickets.AsNoTracking(), pageNumber ?? 1, pageSize));
        }
        // ============================================================
        // 2. MÀN HÌNH XỬ LÝ (GET) - Load thông tin & Lịch sử
        // ============================================================
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.TechnicalTickets
                .Include(t => t.Laptop)
                .Include(t => t.Status)
                .Include(t => t.Technical)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            // Placeholder logs
            ViewBag.Logs = new List<string>();

            return View(ticket);
        }

        // ============================================================
        // 3. XỬ LÝ CẬP NHẬT (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            long id,
            long statusId,
            string technicalResponse,
            // Các tham số Checklist QC
            bool qcWifi, bool qcKeyboard, bool qcScreen, bool qcClean,
            // Các tham số Linh kiện
            string? partName, decimal? partPrice, string? partNote
        )
        {
            var originalTicket = await _context.TechnicalTickets
                .Include(t => t.Laptop)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (originalTicket == null) return NotFound();

            try
            {
                // --- LOGIC 1: KIỂM TRA QUY TRÌNH QC (Khi chọn Đã sửa xong - ID 5) ---
                if (statusId == 5)
                {
                    if (!qcWifi || !qcKeyboard || !qcScreen || !qcClean)
                    {
                        ModelState.AddModelError("", "LỖI QC: Bạn chưa hoàn thành quy trình kiểm tra chất lượng.");
                        ViewBag.Logs = new List<string>();
                        return View(originalTicket);
                    }
                    technicalResponse += " \n[SYSTEM]: Đã thông qua kiểm tra QC (Wifi: OK, Key: OK, Screen: OK).";
                }

                // --- LOGIC 2: XỬ LÝ YÊU CẦU LINH KIỆN (Khi chọn Chờ linh kiện - ID 6 từ View) ---
                if (statusId == 6)
                {
                    // 2.1. Validate dữ liệu đầu vào
                    if (string.IsNullOrEmpty(partName))
                    {
                        ModelState.AddModelError("", "LỖI VẬT TƯ: Vui lòng nhập tên linh kiện cần thay thế.");
                        ViewBag.Logs = new List<string>();
                        return View(originalTicket);
                    }

                    // 2.2. Ghi Log chi tiết vào lịch sử
                    string note = string.IsNullOrEmpty(partNote) ? "" : $" (Ghi chú: {partNote})";
                    string price = partPrice.HasValue ? $"{partPrice:N0} VNĐ" : "Chưa báo giá";

                    technicalResponse += $" \n[REQUEST]: Yêu cầu linh kiện: {partName} - Giá: {price}{note}";
                    technicalResponse += " \n[SYSTEM]: Đã ghi nhận yêu cầu vật tư. Trạng thái tự động chuyển về 'Fixing' (Đang sửa chữa).";

                    // 2.3. QUAN TRỌNG: Ép trạng thái quay về "Fixing" (ID 4)
                    statusId = 4;
                }

                // --- CẬP NHẬT DỮ LIỆU ---
                originalTicket.StatusId = statusId;
                originalTicket.TechnicalResponse = technicalResponse;
                originalTicket.UpdatedDate = DateTime.Now;

                // --- ĐỒNG BỘ TRẠNG THÁI LAPTOP (CẬP NHẬT ID MỚI) ---
                var relatedLaptop = await _context.Laptops.FindAsync(originalTicket.LaptopId);
                if (relatedLaptop != null)
                {
                    switch (statusId)
                    {
                        case 4: // Fixing (Đang sửa)
                            relatedLaptop.StatusId = 4;
                            break;
                        case 5: // Fixed (Đã sửa xong) -> Laptop chuyển trạng thái Available (ID 9)
                            relatedLaptop.StatusId = 9; // <--- SỬA THÀNH 9
                            break;
                        case 11: // Broken (Hư hỏng) -> Laptop chuyển trạng thái Broken (ID 11)
                            relatedLaptop.StatusId = 11; // <--- SỬA THÀNH 11
                            break;
                    }
                    _context.Update(relatedLaptop);
                }

                _context.Update(originalTicket);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cập nhật Ticket #{id} thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                ViewBag.Logs = new List<string>();
                return View(originalTicket);
            }
        }

        // ============================================================
        // 4. CHỨC NĂNG PHỤ: THÊM NHẬT KÝ (QUICK LOG)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> AddQuickLog(long ticketId, string logMessage)
        {
            if (string.IsNullOrWhiteSpace(logMessage))
            {
                return RedirectToAction("Edit", new { id = ticketId });
            }

            var ticket = await _context.TechnicalTickets.FindAsync(ticketId);
            if (ticket != null)
            {
                string timeStamp = DateTime.Now.ToString("dd/MM HH:mm");
                string newLog = $"\n[{timeStamp}] Log: {logMessage}";

                ticket.TechnicalResponse = (ticket.TechnicalResponse ?? "") + newLog;

                _context.Update(ticket);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Edit", new { id = ticketId });
        }
    }
}