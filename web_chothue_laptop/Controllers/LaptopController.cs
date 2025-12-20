using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.Helpers;

namespace web_chothue_laptop.Controllers
{
    public class LaptopController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<LaptopController> _logger;

        public LaptopController(Swp391LaptopContext context, ILogger<LaptopController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Laptop/Details/5
        public async Task<IActionResult> Details(long? id, int? page)
        {
            if (id == null)
            {
                return NotFound();
            }

            const int PageSize = 6;
            int pageIndex = page ?? 1;

            var laptop = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.LaptopDetails)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (laptop == null)
            {
                return NotFound();
            }

            // Phân trang cho lịch sử sử dụng (bookings)
            var bookingsQuery = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Status)
                .Where(b => b.LaptopId == laptop.Id)
                .OrderByDescending(b => b.StartTime);

            var paginatedBookings = await PaginatedList<Booking>.CreateAsync(bookingsQuery, pageIndex, PageSize);

            // Kiểm tra booking của user hiện tại (nếu đã đăng nhập)
            var userId = HttpContext.Session.GetString("UserId");
            bool hasPendingBooking = false;
            bool hasActiveBooking = false;
            Booking? activeBooking = null;
            Booking? currentCustomerBooking = null;
            Customer? currentCustomer = null;

            if (!string.IsNullOrEmpty(userId))
            {
                var userIdLong = long.Parse(userId);
                currentCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

                if (currentCustomer != null)
                {
                    // Lấy booking mới nhất đang active của customer cho laptop này (pending, approved, rented)
                    // Không lấy completed/cancelled vì những đơn đó đã hoàn thành, customer có thể đặt lại
                    currentCustomerBooking = await _context.Bookings
                        .Include(b => b.Status)
                        .Where(b => b.CustomerId == currentCustomer.Id 
                            && b.LaptopId == laptop.Id
                            && (b.StatusId == 1 || b.StatusId == 2 || b.StatusId == 10))
                        .OrderByDescending(b => b.CreatedDate)
                        .FirstOrDefaultAsync();

                    // Kiểm tra booking pending
                    hasPendingBooking = await _context.Bookings
                        .Include(b => b.Status)
                        .AnyAsync(b => b.CustomerId == currentCustomer.Id 
                            && b.LaptopId == laptop.Id 
                            && b.StatusId == 1);

                    // Kiểm tra booking active
                    activeBooking = await _context.Bookings
                        .Include(b => b.Status)
                        .Where(b => b.CustomerId == currentCustomer.Id 
                            && b.LaptopId == laptop.Id 
                            && (b.StatusId == 2 || b.StatusId == 10)
                            && b.EndTime >= DateTime.Today)
                        .FirstOrDefaultAsync();

                    hasActiveBooking = activeBooking != null;
                }
            }

            // Kiểm tra xem laptop có đang được người khác thuê không (bất kỳ ai)
            // Chỉ hiển thị "đang thuê" nếu không phải là booking của customer hiện tại
            var isRentedByOthers = await _context.Bookings
                .AnyAsync(b => b.LaptopId == laptop.Id
                    && (b.StatusId == 2 || b.StatusId == 10)
                    && b.StartTime <= DateTime.Now
                    && b.EndTime >= DateTime.Now
                    && (currentCustomer == null || b.CustomerId != currentCustomer.Id));

            // Kiểm tra nếu laptop đã hết thời hạn thuê (EndTime < DateTime.Now)
            var isExpired = laptop.EndTime.HasValue && laptop.EndTime.Value < DateTime.Now;

            ViewBag.HasPendingBooking = hasPendingBooking;
            ViewBag.HasActiveBooking = hasActiveBooking;
            ViewBag.ActiveBooking = activeBooking;
            ViewBag.CurrentCustomerBooking = currentCustomerBooking;
            ViewBag.CurrentCustomer = currentCustomer;
            ViewBag.CurrentUserId = userId;
            ViewBag.IsRentedByOthers = isRentedByOthers;
            ViewBag.IsExpired = isExpired;
            ViewBag.PaginatedBookings = paginatedBookings;
            ViewBag.PageIndex = paginatedBookings.PageIndex;
            ViewBag.TotalPages = paginatedBookings.TotalPages;
            ViewBag.HasPreviousPage = paginatedBookings.HasPreviousPage;
            ViewBag.HasNextPage = paginatedBookings.HasNextPage;

            return View(laptop);
        }
    }
}


