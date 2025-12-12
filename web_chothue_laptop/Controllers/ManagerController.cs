using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using web_chothue_laptop.Models;

public class ManagerController : Controller
{
    private readonly Swp391LaptopContext _context;

    public ManagerController(Swp391LaptopContext context)
    {
        _context = context;
    }

    public IActionResult LaptopRequests(string searchString, int? statusId, int page = 1, int pageSize = 5)
    {
        var query = _context.Laptops
            .Include(l => l.Brand)
            .Include(l => l.Status)
            .Include(l => l.Student)
            .OrderByDescending(l => l.CreatedDate)
            .AsQueryable();

        // Search
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(l => l.Name.Contains(searchString));
        }

        // Filter Status
        if (statusId.HasValue && statusId.Value != 0)
        {
            query = query.Where(l => l.StatusId == statusId.Value);
        }

        // Pagination
        int totalItems = query.Count();
        int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var laptops = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // ViewBag
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchString = searchString;
        ViewBag.SelectedStatus = statusId ?? 0;

        // Status dropdown
        ViewBag.StatusList = _context.Statuses
            .Select(s => new { s.Id, s.StatusName })
            .ToList();

        return View(laptops);
    }


    // APPROVE (toggle giữa Approved <-> Pending)
    [HttpPost]
    public IActionResult Approve(long id)
    {
        var laptop = _context.Laptops.FirstOrDefault(x => x.Id == id);
        if (laptop == null) return NotFound();

        // Nếu đang Approved => chuyển về Pending
        // Nếu đang Pending hoặc Reject => chuyển thành Approved
        laptop.StatusId = laptop.StatusId == 2 ? 1 : 2;
        laptop.UpdatedDate = DateTime.Now;

        _context.SaveChanges();
        return RedirectToAction("LaptopRequests");
    }


    // REJECT (toggle giữa Rejected <-> Pending)
    [HttpPost]
    public IActionResult Reject(long id)
    {
        var laptop = _context.Laptops.FirstOrDefault(x => x.Id == id);
        if (laptop == null) return NotFound();

        // Nếu đang Rejected => chuyển về Pending
        // Nếu đang Pending hoặc Approved => chuyển thành Rejected
        laptop.StatusId = laptop.StatusId == 3 ? 1 : 3;
        laptop.UpdatedDate = DateTime.Now;

        _context.SaveChanges();
        return RedirectToAction("LaptopRequests");
    }


    // Laptop Detail
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
