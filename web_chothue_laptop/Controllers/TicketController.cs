using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.Services;
using web_chothue_laptop.ViewModels;

namespace web_chothue_laptop.Controllers
{
    public class TicketController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly CloudinaryService _cloudinaryService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(Swp391LaptopContext context, CloudinaryService cloudinaryService, ILogger<TicketController> logger)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        // GET: Ticket/Create/5?bookingId=xxx
        public async Task<IActionResult> Create(long? id, long? bookingId)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để báo lỗi thiết bị.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var laptop = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (laptop == null)
            {
                return NotFound();
            }

            // Lấy Customer từ UserId
            var userIdLong = long.Parse(userId);
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra booking (nếu có)
            Booking? booking = null;
            if (bookingId.HasValue)
            {
                booking = await _context.Bookings
                    .Include(b => b.Status)
                    .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id && b.LaptopId == laptop.Id);
            }

            var model = new TicketViewModel
            {
                LaptopId = laptop.Id,
                BookingId = booking?.Id
            };

            ViewBag.Laptop = laptop;
            ViewBag.Booking = booking;

            return View(model);
        }

        // POST: Ticket/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketViewModel model)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để báo lỗi thiết bị.";
                return RedirectToAction("Login", "Account");
            }

            // Load lại laptop
            var laptop = await _context.Laptops
                .Include(l => l.Brand)
                .Include(l => l.Status)
                .FirstOrDefaultAsync(m => m.Id == model.LaptopId);

            if (laptop == null)
            {
                return NotFound();
            }

            ViewBag.Laptop = laptop;

            if (!ModelState.IsValid)
            {
                // Load booking nếu có
                if (model.BookingId.HasValue)
                {
                    var userIdLong = long.Parse(userId);
                    var customer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);
                    
                    if (customer != null)
                    {
                        var booking = await _context.Bookings
                            .Include(b => b.Status)
                            .FirstOrDefaultAsync(b => b.Id == model.BookingId.Value && b.CustomerId == customer.Id);
                        ViewBag.Booking = booking;
                    }
                }
                return View(model);
            }

            // Lấy Customer từ UserId
            var userIdLong2 = long.Parse(userId);
            var customer2 = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == userIdLong2);

            if (customer2 == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy Staff có StaffId = 6 (staff mới được tạo)
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == 6);
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Không tìm thấy nhân viên xử lý.";
                return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
            }

            // Upload ảnh lỗi lên Cloudinary (nếu có)
            string? errorImageUrl = null;
            if (model.ErrorImage != null && model.ErrorImage.Length > 0)
            {
                try
                {
                    errorImageUrl = await _cloudinaryService.UploadImageAsync(model.ErrorImage, "tickets");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error uploading ticket image");
                    ModelState.AddModelError("ErrorImage", "Không thể upload ảnh. Vui lòng thử lại.");
                    return View(model);
                }
            }

            // Lấy StatusId cho ticket mới (pending)
            var statusId = await GetStatusIdAsync("pending");
            if (statusId == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
            }

            // Tạo TechnicalTicket placeholder (bắt buộc vì TechnicalTicketId là required)
            // Staff sẽ cập nhật hoặc tạo TechnicalTicket mới khi xử lý
            var technicalTicket = new TechnicalTicket
            {
                LaptopId = model.LaptopId,
                BookingId = model.BookingId,
                StaffId = staff.Id,
                Description = "Chờ Staff xử lý - Ticket từ Customer", // Placeholder, Staff sẽ cập nhật
                StatusId = statusId.Value,
                CreatedDate = DateTime.Now
            };

            _context.TechnicalTickets.Add(technicalTicket);
            await _context.SaveChangesAsync();

            // Chỉ tạo TicketList - gửi cho Staff xử lý
            // Staff sẽ xử lý và cập nhật TechnicalTicket sau
            var ticketList = new TicketList
            {
                CustomerId = customer2.Id,
                StaffId = staff.Id,
                LaptopId = model.LaptopId,
                TechnicalTicketId = technicalTicket.Id, // Link với TechnicalTicket placeholder
                Description = model.Description, // Mô tả lỗi từ Customer
                ErrorImageUrl = errorImageUrl,
                FixedCost = 0, // Sẽ được cập nhật sau khi xử lý
                StatusId = statusId.Value,
                CreatedDate = DateTime.Now
            };

            _context.TicketLists.Add(ticketList);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đơn báo lỗi đã được gửi đi. Quý khách vui lòng chờ để được tiếp nhận xử lý nhanh nhất có thể.";
            return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
        }

        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }
}

