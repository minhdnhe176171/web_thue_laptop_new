using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Swp391LaptopContext _context;

        public HomeController(ILogger<HomeController> logger, Swp391LaptopContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            // if no db context available, fall back to empty view
            if (_context == null) return View(Enumerable.Empty<Laptop>());

            var q = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Include(l => l.LaptopDetails)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(l => (l.Name ?? "").ToLower().Contains(s) || (l.Brand!.BrandName ?? "").ToLower().Contains(s));
            }

            var list = await q.OrderByDescending(l => l.CreatedDate).ToListAsync();
            ViewBag.Search = search;
            return View(list);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
