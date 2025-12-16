using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Controllers
{
    public class ManagerController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(Swp391LaptopContext context, ILogger<ManagerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Manager/CustomerManagement
        // Màn hình 1: Danh sách Customer
        public async Task<IActionResult> CustomerManagement()
        {
            var customers = await _context.Customers
                .Include(c => c.CustomerNavigation)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            return View(customers);
        }

        // GET: Manager/RentalHistory/{id}
        // Màn hình 2: Lịch sử thuê của Customer
        public async Task<IActionResult> RentalHistory(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(c => c.CustomerNavigation)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            var bookings = await _context.Bookings
                .Include(b => b.Laptop)
                .Include(b => b.Status)
                .Include(b => b.Staff)
                .Where(b => b.CustomerId == id)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            ViewBag.Customer = customer;
            ViewBag.Bookings = bookings;

            return View(bookings);
        }

        // POST: Manager/ToggleBlacklist/{id}
        // Bật/tắt trạng thái Blacklist của Customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlacklist(long id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            // Toggle blacklist status
            customer.BlackList = !(customer.BlackList ?? false);
            
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = customer.BlackList == true 
                    ? "Đã thêm customer vào blacklist thành công!" 
                    : "Đã xóa customer khỏi blacklist thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling blacklist for customer {CustomerId}", id);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái blacklist.";
            }

            return RedirectToAction(nameof(RentalHistory), new { id = id });
        }
    }
}

