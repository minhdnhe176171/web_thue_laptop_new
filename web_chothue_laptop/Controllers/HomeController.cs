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

        public async Task<IActionResult> Index(int? page, string? search, string? brand)
        {
            int pageIndex = page ?? 1;
            
            // Lấy TẤT CẢ laptop ID đang có người thuê (bất kỳ user nào) - StatusId = 2 (Approved) hoặc 10 (Rented)
            // Điều kiện: StartTime <= hiện tại và EndTime >= hiện tại (đang trong thời gian thuê)
            var rentedLaptopIds = await _context.Bookings
                .Where(b => (b.StatusId == 2 || b.StatusId == 10)
                    && b.StartTime <= DateTime.Now 
                    && b.EndTime >= DateTime.Now)
                .Select(b => b.LaptopId)
                .Distinct()
                .ToListAsync();
            
            // Lấy TẤT CẢ laptop có status "available" (không lọc bỏ laptop đang thuê)
            var allLaptopsQuery = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .Where(l => l.Status != null && l.Status.StatusName.ToLower() == "available");

            // Filter theo tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                allLaptopsQuery = allLaptopsQuery.Where(l => 
                    l.Name.Contains(search) || 
                    (l.Brand != null && l.Brand.BrandName.Contains(search)));
            }

            // Filter theo brand
            if (!string.IsNullOrWhiteSpace(brand))
            {
                allLaptopsQuery = allLaptopsQuery.Where(l => 
                    l.Brand != null && l.Brand.BrandName.ToLower() == brand.ToLower());
            }

            allLaptopsQuery = allLaptopsQuery
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

            // Lấy danh sách brands để hiển thị filter
            var brands = await _context.Brands
                .OrderBy(b => b.BrandName)
                .ToListAsync();

            // Truyền danh sách laptop ID đang thuê vào ViewBag để view có thể hiển thị status
            ViewBag.RentedLaptopIds = rentedLaptopIds;
            ViewBag.CurrentBookings = currentBookings;
            ViewBag.PageIndex = paginatedLaptops.PageIndex;
            ViewBag.TotalPages = paginatedLaptops.TotalPages;
            ViewBag.HasPreviousPage = paginatedLaptops.HasPreviousPage;
            ViewBag.HasNextPage = paginatedLaptops.HasNextPage;
            ViewBag.Search = search;
            ViewBag.Brand = brand;
            ViewBag.Brands = brands;

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

