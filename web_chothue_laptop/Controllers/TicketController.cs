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

            // Kiểm tra booking - bắt buộc phải có active booking
            Booking? booking = null;
            if (bookingId.HasValue)
            {
                booking = await _context.Bookings
                    .Include(b => b.Status)
                    .FirstOrDefaultAsync(b => b.Id == bookingId.Value && b.CustomerId == customer.Id && b.LaptopId == laptop.Id);
            }
            else
            {
                // Nếu không có bookingId, tìm active booking của customer này
                booking = await _context.Bookings
                    .Include(b => b.Status)
                    .Where(b => b.CustomerId == customer.Id 
                        && b.LaptopId == laptop.Id 
                        && (b.StatusId == 2 || b.StatusId == 10)
                        && b.EndTime >= DateTime.Today)
                    .FirstOrDefaultAsync();
            }

            // Kiểm tra có active booking không - chỉ khi đang thuê mới được báo lỗi
            if (booking == null || booking.StatusId != 2 && booking.StatusId != 10 || 
                booking.EndTime < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Chỉ có thể báo lỗi khi bạn đang thuê sản phẩm này.";
                return RedirectToAction("Details", "Laptop", new { id = id });
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
                        var bookingForView = await _context.Bookings
                            .Include(b => b.Status)
                            .FirstOrDefaultAsync(b => b.Id == model.BookingId.Value && b.CustomerId == customer.Id);
                        ViewBag.Booking = bookingForView;
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

            // Kiểm tra có active booking không
            Booking? booking = null;
            if (model.BookingId.HasValue)
            {
                booking = await _context.Bookings
                    .Include(b => b.Status)
                    .FirstOrDefaultAsync(b => b.Id == model.BookingId.Value && b.CustomerId == customer2.Id && b.LaptopId == laptop.Id);
            }
            else
            {
                // Nếu không có bookingId, tìm active booking của customer này
                booking = await _context.Bookings
                    .Include(b => b.Status)
                    .Where(b => b.CustomerId == customer2.Id 
                        && b.LaptopId == laptop.Id 
                        && (b.StatusId == 2 || b.StatusId == 10)
                        && b.EndTime >= DateTime.Today)
                    .FirstOrDefaultAsync();
            }

            if (booking == null || booking.StatusId != 2 && booking.StatusId != 10 || 
                booking.EndTime < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Chỉ có thể báo lỗi khi bạn đang thuê sản phẩm này.";
                return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
            }

            // Cập nhật model.BookingId nếu chưa có
            if (!model.BookingId.HasValue && booking != null)
            {
                model.BookingId = booking.Id;
            }

            // Lấy Technical từ User có role "technical" để xử lý ticket
            var technicalRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName.ToLower() == "technical");
            if (technicalRole == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Không tìm thấy role technical.";
                return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
            }

            // Lấy User có role technical
            var technicalUser = await _context.Users
                .Include(u => u.Technicals)
                .FirstOrDefaultAsync(u => u.RoleId == technicalRole.Id);

            if (technicalUser == null || !technicalUser.Technicals.Any())
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Không tìm thấy kỹ thuật viên để xử lý.";
                return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
            }

            var technical = technicalUser.Technicals.First();

            // Lấy Staff để gán vào StaffId (required field) - có thể lấy staff đầu tiên hoặc staff mặc định
            var staff = await _context.Staff.FirstOrDefaultAsync();
            if (staff == null)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống. Không tìm thấy nhân viên.";
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

            // Tạo TechnicalTicket với StatusId = 1 (Pending) và gán Technical để xử lý
            var technicalTicket = new TechnicalTicket
            {
                LaptopId = model.LaptopId,
                BookingId = model.BookingId,
                StaffId = staff.Id, // Required field
                TechnicalId = technical.Id, // Gán Technical để xử lý
                Description = model.Description, // Mô tả lỗi từ Customer
                StatusId = 1, // Pending (ID 1)
                CreatedDate = DateTime.Now
            };

            _context.TechnicalTickets.Add(technicalTicket);
            await _context.SaveChangesAsync();

            // Tạo TicketList với StatusId = 1 (Pending)
            var ticketList = new TicketList
            {
                CustomerId = customer2.Id,
                StaffId = staff.Id,
                LaptopId = model.LaptopId,
                TechnicalTicketId = technicalTicket.Id,
                Description = model.Description, // Mô tả lỗi từ Customer
                ErrorImageUrl = errorImageUrl,
                FixedCost = 0, // Bỏ qua logic chi phí
                StatusId = 1, // Pending (ID 1)
                CreatedDate = DateTime.Now
            };

            _context.TicketLists.Add(ticketList);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đơn báo lỗi đã được gửi đi. Quý khách vui lòng chờ để được tiếp nhận xử lý nhanh nhất có thể.";
            return RedirectToAction("Details", "Laptop", new { id = model.LaptopId });
        }

        // GET: Ticket/MyTickets - Lịch sử báo lỗi
        public async Task<IActionResult> MyTickets()
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem lịch sử báo lỗi.";
                return RedirectToAction("Login", "Account");
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

            // Lấy tất cả ticket của customer này, sắp xếp theo ngày tạo mới nhất
            var tickets = await _context.TicketLists
                .Include(t => t.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(t => t.Status)
                .Include(t => t.TechnicalTicket)
                    .ThenInclude(tt => tt.Status)
                .Where(t => t.CustomerId == customer.Id)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            ViewBag.Tickets = tickets;
            ViewBag.CustomerId = customer.Id;

            return View();
        }

        // GET: Ticket/Details/5 - Chi tiết ticket
        public async Task<IActionResult> Details(long? id)
        {
            // Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem chi tiết ticket.";
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
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

            // Lấy ticket với đầy đủ thông tin (bao gồm Technical)
            var ticket = await _context.TicketLists
                .Include(t => t.Laptop)
                    .ThenInclude(l => l.Brand)
                .Include(t => t.Status)
                .Include(t => t.TechnicalTicket)
                    .ThenInclude(tt => tt.Status)
                .Include(t => t.TechnicalTicket)
                    .ThenInclude(tt => tt.Technical)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customer.Id);

            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy ticket.";
                return RedirectToAction("MyTickets");
            }

            // Lấy timeline trạng thái từ TechnicalTicket
            var statusHistory = new List<StatusHistoryItem>();
            
            // Pending (1) - Mặc định khi tạo
            statusHistory.Add(new StatusHistoryItem
            {
                StatusId = 1,
                StatusName = "Pending",
                StatusVi = "Mới tạo",
                IsActive = ticket.TechnicalTicket?.StatusId == 1,
                IsCompleted = ticket.TechnicalTicket?.StatusId > 1
            });

            // Approved (2)
            statusHistory.Add(new StatusHistoryItem
            {
                StatusId = 2,
                StatusName = "Approved",
                StatusVi = "Kỹ thuật đã nhận",
                IsActive = ticket.TechnicalTicket?.StatusId == 2,
                IsCompleted = ticket.TechnicalTicket?.StatusId > 2
            });

            // Fixing (4)
            statusHistory.Add(new StatusHistoryItem
            {
                StatusId = 4,
                StatusName = "Fixing",
                StatusVi = "Đang sửa",
                IsActive = ticket.TechnicalTicket?.StatusId == 4,
                IsCompleted = ticket.TechnicalTicket?.StatusId > 4
            });

            // Fixed (5)
            statusHistory.Add(new StatusHistoryItem
            {
                StatusId = 5,
                StatusName = "Fixed",
                StatusVi = "Đã sửa xong",
                IsActive = ticket.TechnicalTicket?.StatusId == 5,
                IsCompleted = ticket.TechnicalTicket?.StatusId == 5
            });

            ViewBag.Ticket = ticket;
            ViewBag.StatusHistory = statusHistory;

            return View();
        }

        private async Task<long?> GetStatusIdAsync(string statusName)
        {
            var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
            return status?.Id;
        }
    }

    // Helper class cho Status History
    public class StatusHistoryItem
    {
        public long StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string StatusVi { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
    }
}

