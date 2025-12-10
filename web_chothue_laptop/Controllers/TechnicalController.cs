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
        // Trong method Index của TechnicalController.cs
        public async Task<IActionResult> Index(int? pageNumber, string? searchString)
        {
            // --- THỐNG KÊ DASHBOARD ---
            ViewData["BrokenCount"] = await _context.TechnicalTickets.CountAsync(t => t.StatusId == 3);
            ViewData["ProcessingCount"] = await _context.TechnicalTickets.CountAsync(t => t.StatusId == 4);
            ViewData["FixedCount"] = await _context.TechnicalTickets.CountAsync(t => t.StatusId == 5);

            // MỚI: Tính tổng số lượng ticket
            ViewData["TotalCount"] = await _context.TechnicalTickets.CountAsync();

            // ... (Các phần code truy vấn và search giữ nguyên) ...

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

            tickets = tickets.OrderByDescending(t => t.StatusId == 3)
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

            // Gửi danh sách Log (Nhật ký) sang View (Nếu chưa có bảng Log thì dùng tạm List rỗng)
            // Khi nào có bảng TicketLogs thì bỏ comment dòng dưới:
            // ViewBag.Logs = await _context.TicketLogs.Where(l => l.TicketId == id).OrderByDescending(l => l.CreatedAt).ToListAsync();
            ViewBag.Logs = new List<string>(); // Placeholder để tránh lỗi View

            return View(ticket);
        }

        // ============================================================
        // 3. XỬ LÝ CẬP NHẬT (POST) - Logic nghiệp vụ phức tạp
        // ============================================================
        // 3. XỬ LÝ CẬP NHẬT (POST)
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
                // --- LOGIC 1: KIỂM TRA QUY TRÌNH QC (Khi chọn Đã sửa xong) ---
                if (statusId == 5)
                {
                    if (!qcWifi || !qcKeyboard || !qcScreen || !qcClean)
                    {
                        ModelState.AddModelError("", "LỖI QC: Bạn chưa hoàn thành quy trình kiểm tra chất lượng.");
                        // Trả về View để tick lại
                        // (Quan trọng: Cần load lại ViewBag.Logs nếu dùng logic logs riêng)
                        ViewBag.Logs = new List<string>();
                        return View(originalTicket);
                    }
                    technicalResponse += " \n[SYSTEM]: Đã thông qua kiểm tra QC (Wifi: OK, Key: OK, Screen: OK).";
                }

                // --- LOGIC 2: XỬ LÝ YÊU CẦU LINH KIỆN (Khi chọn Chờ linh kiện - ID 6) ---
                // SỬA ĐỔI TẠI ĐÂY THEO YÊU CẦU CỦA BẠN
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
                    technicalResponse += " \n[SYSTEM]: Đã ghi nhận yêu cầu vật tư. Trạng thái tự động chuyển về 'Đang sửa chữa'.";

                    // 2.3. QUAN TRỌNG: Ép trạng thái quay về "Đang sửa chữa" (ID 4)
                    statusId = 4;
                }

                // --- CẬP NHẬT DỮ LIỆU ---
                originalTicket.StatusId = statusId; // Nếu là linh kiện (6) thì ở dòng trên đã bị đổi thành (4)
                originalTicket.TechnicalResponse = technicalResponse;
                originalTicket.UpdatedDate = DateTime.Now;

                // --- ĐỒNG BỘ TRẠNG THÁI LAPTOP ---
                var relatedLaptop = await _context.Laptops.FindAsync(originalTicket.LaptopId);
                if (relatedLaptop != null)
                {
                    switch (statusId)
                    {
                        case 4: // Đang sửa (Bao gồm cả trường hợp vừa order linh kiện xong)
                            relatedLaptop.StatusId = 4; // Laptop màu Vàng
                            break;
                        case 5: // Đã sửa xong
                            relatedLaptop.StatusId = 1; // Laptop màu Xanh (Sẵn sàng)
                            break;
                        case 3: // Hư hỏng
                            relatedLaptop.StatusId = 3; // Laptop màu Đỏ
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
        // 4. CHỨC NĂNG PHỤ: THÊM NHẬT KÝ (QUICK LOG) - Method 1
        // ============================================================
        // Action này dùng để KTV ghi chú nhanh mà không cần đổi trạng thái
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
                // Vì chưa có bảng Log riêng, ta sẽ nối vào TechnicalResponse hiện tại
                // Format: [Thời gian] Nội dung
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