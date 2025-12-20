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
            
            // Lấy customer hiện tại từ session (nếu có)
            Customer? currentCustomer = null;
            var userId = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userId))
            {
                var userIdLong = long.Parse(userId);
                currentCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);
            }
            
            // Lấy TẤT CẢ laptop ID đang có người thuê (bất kỳ user nào) - StatusId = 2 (Approved), 10 (Rented), hoặc 12 (Banked - đã chuyển khoản)
            // Điều kiện: EndTime >= hiện tại (chưa hết hạn thuê) - bao gồm cả trường hợp đã chuyển khoản nhưng chưa đến ngày lấy máy
            var rentedLaptopIds = await _context.Bookings
                .Where(b => (b.StatusId == 2 || b.StatusId == 10 || b.StatusId == 12) // Approved, Rented, hoặc Banked (đã chuyển khoản)
                    && b.EndTime >= DateTime.Today)
                .Select(b => b.LaptopId)
                .Distinct()
                .ToListAsync();
            
            // Lấy TẤT CẢ laptop trong database (không filter theo status)
            IQueryable<Laptop> allLaptopsQuery = _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status);

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

            // Lấy booking của customer hiện tại cho từng laptop để hiển thị trạng thái đúng
            // Chỉ lấy booking đang active (pending, approved, rented) - không lấy completed/cancelled
            Dictionary<long, Booking?> customerBookings = new Dictionary<long, Booking?>();
            if (currentCustomer != null)
            {
                var laptopIds = paginatedLaptops.Select(l => l.Id).ToList();
                // Chỉ lấy booking đang active: pending (1), approved (2), rented (10), banked (12 - đã chuyển khoản)
                var bookings = await _context.Bookings
                    .Include(b => b.Status)
                    .Where(b => b.CustomerId == currentCustomer.Id 
                        && laptopIds.Contains(b.LaptopId)
                        && (b.StatusId == 1 || b.StatusId == 2 || b.StatusId == 10 || b.StatusId == 12))
                    .OrderByDescending(b => b.CreatedDate)
                    .ToListAsync();
                
                foreach (var laptop in paginatedLaptops)
                {
                    // Tìm booking mới nhất đang active của customer cho laptop này
                    var booking = bookings
                        .Where(b => b.LaptopId == laptop.Id)
                        .OrderByDescending(b => b.CreatedDate)
                        .FirstOrDefault();
                    customerBookings[laptop.Id] = booking;
                }
            }

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
            ViewBag.CustomerBookings = customerBookings;
            ViewBag.CurrentCustomer = currentCustomer;
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

