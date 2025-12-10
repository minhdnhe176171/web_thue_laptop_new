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

        // 1.Dashboard
        public async Task<IActionResult> Index(int? pageNumber, string searchString)
        {
            // Lưu lại từ khóa tìm kiếm để hiển thị lại trên View khi chuyển trang
            ViewData["CurrentFilter"] = searchString;

            // 1. Truy vấn cơ bản
            var tickets = _context.TechnicalTickets
                .Include(t => t.Laptop)
                .Include(t => t.Status)
                .Include(t => t.Technical)
                .AsQueryable();

            // 2. Logic Tìm kiếm (Search)
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm theo Mã Ticket (Id là số nên cần chuyển sang string hoặc so sánh trực tiếp)
                // Dùng ToString() để cho phép tìm kiếm dạng text "1", "2"...
                tickets = tickets.Where(t => t.Id.ToString().Contains(searchString));
            }

            // 3. Sắp xếp: Mới nhất lên đầu
            tickets = tickets.OrderByDescending(t => t.CreatedDate);

            int pageSize = 4;

            return View(await PaginatedList<TechnicalTicket>.CreateAsync(tickets.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // 2.Form xử lý
        public async Task<IActionResult> Edit(long? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.TechnicalTickets
                .Include(t => t.Laptop)
                .Include(t => t.Status)
                .Include(t => t.Technical)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();


            return View(ticket);
        }

        // 3.Lưu cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, long statusId, string technicalResponse)
        {
            var originalTicket = await _context.TechnicalTickets
                .Include(t => t.Laptop)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (originalTicket == null) return NotFound();

            try
            {
                // Cập nhật Ticket
                originalTicket.StatusId = statusId;
                originalTicket.TechnicalResponse = technicalResponse;
                originalTicket.UpdatedDate = DateTime.Now;

                // Đồng bộ trạng thái Laptop
                var relatedLaptop = await _context.Laptops.FindAsync(originalTicket.LaptopId);

                if (relatedLaptop != null)
                {
                    switch (statusId)
                    {
                        case 4: // Đang sửa -> Vàng
                            relatedLaptop.StatusId = 4;
                            break;
                        case 5: // Đã sửa xong -> Xanh
                            relatedLaptop.StatusId = 1;
                            break;
                        case 3: // Hư hỏng -> Đỏ
                            relatedLaptop.StatusId = 3;
                            break;
                    }
                    _context.Update(relatedLaptop);
                }

                _context.Update(originalTicket);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(originalTicket);
            }
        }
    }
}