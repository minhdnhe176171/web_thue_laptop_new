using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using System;
using System.Linq;

public class ManagerController : Controller
{
    private readonly Swp391LaptopContext _context;

    public ManagerController(Swp391LaptopContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        // Có thể để trống hoặc truyền số liệu dashboard sau
        return View();
    }
    // 1. Quản lý toàn bộ Laptop
    public IActionResult LaptopManagement(string searchString, int? statusId, int page = 1)
    {
        int pageSize = 5;

        var query = _context.Laptops
            .Include(l => l.Brand)
            .Include(l => l.Status)
            .Include(l => l.Student)
            .Where(l => l.StatusId == 4
                     || l.StatusId == 8
                     || l.StatusId == 9
                     || l.StatusId == 10)
            .OrderByDescending(l => l.CreatedDate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
            query = query.Where(l => l.Name.Contains(searchString));

        if (statusId.HasValue && statusId.Value != 0)
            query = query.Where(l => l.StatusId == statusId.Value);

        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var laptops = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Paging
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchString = searchString;
        ViewBag.SelectedStatus = statusId ?? 0;

        // Status filter
        ViewBag.StatusList = _context.Statuses
            .Where(s => s.Id == 4 || s.Id == 8 || s.Id == 9 || s.Id == 10)
            .Select(s => new { s.Id, s.StatusName })
            .ToList();

        // Thống kê
        ViewBag.TotalLaptop = _context.Laptops
            .Count(l => l.StatusId == 4 || l.StatusId == 8 || l.StatusId == 9 || l.StatusId == 10);

        ViewBag.RentingLaptop = _context.Laptops.Count(l => l.StatusId == 10);
        ViewBag.MaintenanceLaptop = _context.Laptops.Count(l => l.StatusId == 4);
        ViewBag.AvailableLaptop = _context.Laptops.Count(l => l.StatusId == 9);

        return View(laptops);
    }


    // 2. Quản lý đơn từ Student
    public IActionResult LaptopRequests(string searchString, int? statusId, int page = 1)
    {
        int pageSize = 5; // 👈 cố định 5 bản ghi / trang

        var query = _context.Laptops
            .Include(l => l.Brand)
            .Include(l => l.Status)
            .Include(l => l.Student)
            .Where(l => l.StudentId != null
                     && (l.StatusId == 1 || l.StatusId == 2 || l.StatusId == 3))
            .OrderByDescending(l => l.CreatedDate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
            query = query.Where(l => l.Name.Contains(searchString));

        if (statusId.HasValue && statusId.Value != 0)
            query = query.Where(l => l.StatusId == statusId.Value);

        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var laptops = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchString = searchString;
        ViewBag.SelectedStatus = statusId ?? 0;

        ViewBag.StatusList = _context.Statuses
            .Where(s => s.Id == 1 || s.Id == 2 || s.Id == 3)
            .Select(s => new { s.Id, s.StatusName })
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

        // 🔍 tìm theo email khách hàng hoặc tên laptop
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(b =>
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

        // danh sách status cho dropdown
        ViewBag.StatusList = _context.Statuses
            .Where(s => s.Id == 1 || s.Id == 2 || s.Id == 3 || s.Id == 8 || s.Id == 10)
            .Select(s => new { s.Id, s.StatusName })
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
}
