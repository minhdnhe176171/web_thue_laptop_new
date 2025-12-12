using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
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

        public StaffController(Swp391LaptopContext context)
        {
            _context = context;
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
    }
}