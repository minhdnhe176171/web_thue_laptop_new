using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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

    public IActionResult Index()
    {
        return View();
    }

    // 1. Quản lý toàn bộ Laptop
    public IActionResult LaptopManagement(string searchString, int? statusId, int page = 1)
    {
        int pageSize = 5;

        if (!statusId.HasValue)
        {
            statusId = 9;
        }

        var query = _context.Laptops
            .Include(l => l.Brand)
            .Include(l => l.Status)
            .Include(l => l.Student)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();

            query = query.Where(l =>
                l.Id.ToString().Contains(searchString) ||

                l.Name.Contains(searchString) ||

                (l.Brand != null && l.Brand.BrandName.Contains(searchString)) ||

                (l.Student != null && l.Student.Email.Contains(searchString)) ||
                (l.Student != null && l.Student.FirstName.Contains(searchString)) ||
                (l.Student != null && l.Student.LastName.Contains(searchString)
            )
            );
        }

        if (statusId.HasValue && statusId.Value != 0)
            query = query.Where(l => l.StatusId == statusId.Value);

        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var laptops = query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var statusFromDb = _context.Statuses
            .Where(s => s.Id == 4 ||s.Id == 9 || s.Id == 10)
            .ToList();

        // Paging
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchString = searchString;
        ViewBag.SelectedStatus = statusId;

        // Status filter
        var StatusViMap = new Dictionary<string, string>
            {
                { "Available", "Có sẵn" },
                { "Rented", "Đang thuê" },
                { "Fixing", "Đang sửa chữa" }
               
            };

        ViewBag.StatusList = statusFromDb
            .Select(s => new
            {
                s.Id,
                StatusName = StatusViMap.ContainsKey(s.StatusName)
                    ? StatusViMap[s.StatusName]
                    : s.StatusName
            })
            .ToList();

        // Thống kê (GIỮ NGUYÊN)
        ViewBag.TotalLaptop = _context.Laptops
            .Count(l => l.StatusId == 4 ||l.StatusId == 9 || l.StatusId == 10);

        ViewBag.RentingLaptop = _context.Laptops.Count(l => l.StatusId == 10);
        ViewBag.MaintenanceLaptop = _context.Laptops.Count(l => l.StatusId == 4);
        ViewBag.AvailableLaptop = _context.Laptops.Count(l => l.StatusId == 9);

        return View(laptops);
    }



    // 2. Quản lý đơn từ Student
    public IActionResult LaptopRequests(string searchString, int? statusId, int page = 1)
    {
        int pageSize = 5;

        if (!statusId.HasValue)
        {
            statusId = 1;
        }

        var query = _context.Laptops
            .Include(l => l.Brand)
            .Include(l => l.Status)
            .Include(l => l.Student)
            .Where(l => l.StudentId != null
                     && (l.StatusId == 1 || l.StatusId == 2 || l.StatusId == 3))
            .OrderByDescending(l => l.CreatedDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            searchString = searchString.Trim();

            query = query.Where(l =>
                l.Id.ToString().Contains(searchString) ||

                l.Name.Contains(searchString) ||

                (l.Brand != null && l.Brand.BrandName.Contains(searchString)) ||

                (l.Student != null && l.Student.Email.Contains(searchString)) ||
                (l.Student != null && l.Student.FirstName.Contains(searchString)) ||
                (l.Student != null && l.Student.LastName.Contains(searchString)
                )
                );

        }

        if (statusId.HasValue && statusId.Value != 0)
            query = query.Where(l => l.StatusId == statusId.Value);

        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var laptops = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var statusFromDb = _context.Statuses
            .Where(s => s.Id == 1 || s.Id == 2 || s.Id == 3)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchString = searchString;
        ViewBag.SelectedStatus = statusId ?? 0;

        var StatusViMap = new Dictionary<string, string>
            {
                { "Pending", "Đang chờ" },
                { "Approved", "Đã phê duyêt" },
                { "Rejected", "Đã từ chối" }
            };

        ViewBag.StatusList = statusFromDb
            .Select(s => new
            {
                s.Id,
                StatusName = StatusViMap.ContainsKey(s.StatusName)
                    ? StatusViMap[s.StatusName]
                    : s.StatusName
            })
            .ToList();

        return View(laptops);
    }


    // Duyệt đơn từ Student
    [HttpPost]
    public IActionResult Approve(long id)
    {
        var laptop = _context.Laptops.FirstOrDefault(x => x.Id == id && x.StudentId != null);
        if (laptop == null) return NotFound();

        if (laptop.StatusId == 1 || laptop.StatusId == 3)
        {
            laptop.StatusId = 2; // Approved
            laptop.UpdatedDate = DateTime.Now;
            _context.SaveChanges();
        }
        return RedirectToAction("LaptopRequests");
    }

    // Từ chối đơn từ Student
    [HttpPost]
    public IActionResult Reject(long id)
    {
        var laptop = _context.Laptops.FirstOrDefault(x => x.Id == id && x.StudentId != null);
        if (laptop == null) return NotFound();

        if (laptop.StatusId == 1 || laptop.StatusId == 2)
        {
            laptop.StatusId = 3; // Rejected
            laptop.UpdatedDate = DateTime.Now;
            _context.SaveChanges();
        }
        return RedirectToAction("LaptopRequests");
    }

    // 3. Quản lý đơn từ Customer
    public IActionResult CustomerBookings(string searchString, int? statusId, int page = 1, int pageSize = 5)
    {

        if (!statusId.HasValue)
        {
            statusId = 1;
        }

        var query = _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Laptop)
            .Include(b => b.Status)
            .Where(b => b.StatusId == 1
                     || b.StatusId == 2
                     || b.StatusId == 3
                     || b.StatusId == 8
                     || b.StatusId == 10)
            .OrderByDescending(b => b.CreatedDate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(b =>
                b.Customer.LastName.Contains(searchString) ||
                b.Customer.FirstName.Contains(searchString) ||
                b.Customer.Email.Contains(searchString) ||
                b.Laptop.Name.Contains(searchString));
        }

        // 🔽 lọc trạng thái
        if (statusId.HasValue && statusId.Value != 0)
            query = query.Where(b => b.StatusId == statusId.Value);

        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var bookings = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchString = searchString;
        ViewBag.SelectedStatus = statusId ?? 0;

        var statusFromDb = _context.Statuses
            .Where(s => s.Id == 1 || s.Id == 2 || s.Id == 3 || s.Id == 8 || s.Id == 10)
            .ToList();

        var StatusViMap = new Dictionary<string, string>
            {
                { "Pending", "Đang chờ" },
                { "Approved", "Đã phê duyêt" },
                { "Rejected", "Đã từ chối" },
                { "Close", "Đã đóng"},
                { "Rented", "Đang thuê"}
            };

        ViewBag.StatusList = statusFromDb
            .Select(s => new
            {
                s.Id,
                StatusName = StatusViMap.ContainsKey(s.StatusName)
                    ? StatusViMap[s.StatusName]
                    : s.StatusName
            })
            .ToList();

        // các thống kê GIỮ NGUYÊN
        ViewBag.TotalBooking = _context.Bookings
            .Count(b => b.StatusId == 1 || b.StatusId == 2 || b.StatusId == 3 || b.StatusId == 8 || b.StatusId == 10);
        ViewBag.PendingBooking = _context.Bookings.Count(b => b.StatusId == 1);
        ViewBag.ApprovedBooking = _context.Bookings.Count(b => b.StatusId == 2);
        ViewBag.RejectedBooking = _context.Bookings.Count(b => b.StatusId == 3);
        ViewBag.RentingBooking = _context.Bookings.Count(b => b.StatusId == 10);
        ViewBag.ClosedBooking = _context.Bookings.Count(b => b.StatusId == 8);
        ViewBag.TotalRevenue = _context.Bookings
            .Where(b => b.StatusId == 8)
            .Sum(b => b.TotalPrice);

        return View(bookings);
    }

    // 4. Quản lý nhân viên
    public IActionResult StaffList()
    {
        var staff = _context.Staff.ToList();
        var technical = _context.Technicals.ToList();

        return View(Tuple.Create(staff, technical));
    }

    // ===== CHI TIẾT STAFF =====
    public IActionResult StaffDetail(long id)
    {
        var staff = _context.Staff
            .Include(s => s.Bookings)
            .Include(s => s.BookingReceipts)
            .FirstOrDefault(s => s.Id == id);

        if (staff == null) return NotFound();

        return View(staff);
    }

    // ===== CHI TIẾT TECHNICAL =====
    public IActionResult TechnicalDetail(long id)
    {
        var tech = _context.Technicals
            .Include(t => t.TechnicalTickets)
            .FirstOrDefault(t => t.Id == id);

        if (tech == null) return NotFound();

        return View(tech);
    }

    // Chi tiết Laptop
    public IActionResult LaptopDetail(long id)
    {
        var laptop = _context.Laptops
            .Include(l => l.Brand)
            .Include(l => l.Status)
            .Include(l => l.Student)
            .Include(l => l.LaptopDetails)
            .FirstOrDefault(l => l.Id == id);

        if (laptop == null) return NotFound();

        return View(laptop);
    }

    //GET: Manager/CustomerManagement
    //Màn hình 1: Danh sách Customer
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
        customer.BlackList = !customer.BlackList;

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

    // GET: Manager/SalesReport
    // Màn hình báo cáo doanh số - Các đơn đã Close (Customer đã trả máy)
    public async Task<IActionResult> SalesReport(string? search, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
            // Query các booking đã Close (StatusId = 8)
            var query = _context.Bookings
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(b => b.Laptop)
                    .ThenInclude(l => l.Student)
                .Include(b => b.Customer)
                .Include(b => b.Staff)
                .Include(b => b.Status)
                .Where(b => b.StatusId == 8) // Close - Đã trả máy
                .AsQueryable();

            // Tìm kiếm theo tên laptop, customer, hoặc mã booking
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(b =>
                    b.Id.ToString().Contains(search) ||
                    (b.Laptop != null && b.Laptop.Name.ToLower().Contains(search)) ||
                    (b.Laptop != null && b.Laptop.Brand != null && b.Laptop.Brand.BrandName.ToLower().Contains(search)) ||
                    (b.Customer != null && (
                        b.Customer.FirstName.ToLower().Contains(search) ||
                        b.Customer.LastName.ToLower().Contains(search) ||
                        b.Customer.Email.ToLower().Contains(search)
                    ))
                );
            }

            // Lọc theo ngày
            if (fromDate.HasValue)
            {
                query = query.Where(b => b.UpdatedDate >= fromDate.Value || b.EndTime >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(b => b.UpdatedDate <= endOfDay || b.EndTime <= endOfDay);
            }

            // Thống kê tổng hợp
            var totalRevenue = await query.SumAsync(b => b.TotalPrice ?? 0);
            var totalBookings = await query.CountAsync();
            var avgRevenue = totalBookings > 0 ? totalRevenue / totalBookings : 0;

            // Pagination
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var bookings = await query
                .OrderByDescending(b => b.UpdatedDate ?? b.EndTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Pass data to view
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.AvgRevenue = avgRevenue;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(bookings);
        }
    }
}



