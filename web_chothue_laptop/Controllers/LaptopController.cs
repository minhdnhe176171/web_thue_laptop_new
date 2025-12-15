using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

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
        public async Task<IActionResult> Details(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var laptop = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.LaptopDetails)
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Customer)
                .Include(l => l.Bookings)
                    .ThenInclude(b => b.Status)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (laptop == null)
            {
                return NotFound();
            }

            // Kiểm tra booking của user hiện tại (nếu đã đăng nhập)
            var userId = HttpContext.Session.GetString("UserId");
            bool hasPendingBooking = false;
            bool hasActiveBooking = false;
            Booking? activeBooking = null;

            if (!string.IsNullOrEmpty(userId))
            {
                var userIdLong = long.Parse(userId);
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

                if (customer != null)
                {
                    // Kiểm tra booking pending
                    hasPendingBooking = await _context.Bookings
                        .Include(b => b.Status)
                        .AnyAsync(b => b.CustomerId == customer.Id 
                            && b.LaptopId == laptop.Id 
                            && b.StatusId == 1);

                    // Kiểm tra booking active
                    activeBooking = await _context.Bookings
                        .Include(b => b.Status)
                        .Where(b => b.CustomerId == customer.Id 
                            && b.LaptopId == laptop.Id 
                            && (b.StatusId == 2 || b.StatusId == 10)
                            && b.EndTime >= DateTime.Today)
                        .FirstOrDefaultAsync();

                    hasActiveBooking = activeBooking != null;
                }
            }

            ViewBag.HasPendingBooking = hasPendingBooking;
            ViewBag.HasActiveBooking = hasActiveBooking;
            ViewBag.ActiveBooking = activeBooking;
            ViewBag.CurrentUserId = userId;

            return View(laptop);
        }
    }
}


