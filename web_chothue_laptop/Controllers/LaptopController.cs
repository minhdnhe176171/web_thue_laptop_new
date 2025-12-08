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

            return View(laptop);
        }
    }
}