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
        public async Task<IActionResult> CustomerManagement(string? search, string? filterStatus, int page = 1)
        {
            var query = _context.Customers
                .Include(c => c.CustomerNavigation)
                .AsQueryable();

            // Filter by blacklist status
            if (!string.IsNullOrEmpty(filterStatus))
            {
                if (filterStatus.ToLower() == "blacklist")
                {
                    query = query.Where(c => c.BlackList == true);
                }
                else if (filterStatus.ToLower() == "normal")
                {
                    query = query.Where(c => c.BlackList == null || c.BlackList == false);
                }
            }

            // Search by phone, email, or name
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(c => 
                    c.Email.Contains(search) ||
                    (c.Phone != null && c.Phone.Contains(search)) ||
                    c.FirstName.Contains(search) ||
                    c.LastName.Contains(search));
            }

            // Get total count before pagination
            int totalItems = await query.CountAsync();

            // Pagination
            int pageSize = 6;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var customers = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.FilterStatus = filterStatus;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

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

