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
        // TRANG 1: QUẢN LÝ ĐƠN THUÊ (Mặc định)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            // Chỉ lấy danh sách Booking
            var pendingBookings = await _context.Bookings
                .Include(b => b.Customer).Include(b => b.Laptop)
                .Where(b => b.StatusId == 1) // 1: Pending Approval
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            return View(pendingBookings);
        }

        // ==========================================
        // TRANG 2: MÁY CHỜ KIỂM TRA (Action Mới)
        // ==========================================
        public async Task<IActionResult> LaptopRequests()
        {
            // Lấy danh sách Laptop (Mới hoặc Tech đã duyệt)
            var pendingLaptops = await _context.Laptops
                .Include(l => l.Student)
                .Where(l => l.StatusId == 1 || l.StatusId == 2) // 1: Mới, 2: Đã check OK
                .OrderByDescending(l => l.UpdatedDate)
                .ToListAsync();

            // Trả về View riêng: LaptopRequests.cshtml
            return View(pendingLaptops);
        }

        // ==========================================
        // CÁC HÀM XỬ LÝ BOOKING (Quay về Index)
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBooking(long bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking != null)
            {
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
            if (laptop != null && laptop.StatusId == 2)
            {
                laptop.StatusId = 9; // Available
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã niêm yết máy thành công!";
            }
            // QUAN TRỌNG: Quay lại trang LaptopRequests thay vì Index
            return RedirectToAction(nameof(LaptopRequests));
        }
    }
}