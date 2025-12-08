using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Controllers
{
    public class HomeController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(Swp391LaptopContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách các hãng laptop và laptop theo từng hãng
            var brandsWithLaptops = await _context.Brands
                .Include(b => b.Laptops)
                    .ThenInclude(l => l.Status)
                .Where(b => b.Laptops.Any())
                .ToListAsync();

            // Lấy danh sách booking hiện tại (đang thuê) để hiển thị người thuê
            var currentBookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Status)
                .Where(b => b.StartTime <= DateTime.Now && b.EndTime >= DateTime.Now)
                .OrderByDescending(b => b.StartTime)
                .ToListAsync();

            ViewBag.CurrentBookings = currentBookings;

            return View(brandsWithLaptops);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

