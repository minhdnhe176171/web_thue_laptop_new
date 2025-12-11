using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Controllers
{
    public class HomeController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<HomeController> _logger;
        private const int PageSize = 6;

        public HomeController(Swp391LaptopContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? page)
        {
            int pageIndex = page ?? 1;
            
            // Lấy tất cả laptop để phân trang (bao gồm 8 sản phẩm mới)
            var allLaptopsQuery = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .OrderByDescending(l => l.CreatedDate) // Sắp xếp theo ngày tạo mới nhất
                .ThenBy(l => l.Id);

            var paginatedLaptops = await PaginatedList<Laptop>.CreateAsync(allLaptopsQuery, pageIndex, PageSize);

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
            ViewBag.PageIndex = paginatedLaptops.PageIndex;
            ViewBag.TotalPages = paginatedLaptops.TotalPages;
            ViewBag.HasPreviousPage = paginatedLaptops.HasPreviousPage;
            ViewBag.HasNextPage = paginatedLaptops.HasNextPage;

            return View(paginatedLaptops);
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

