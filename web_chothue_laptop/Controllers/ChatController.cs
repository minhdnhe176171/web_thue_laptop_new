using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Hubs;
using web_chothue_laptop.Models;
using web_chothue_laptop.Services;

namespace web_chothue_laptop.Controllers
{
    public class ChatController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly RedisService _redisService;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(Swp391LaptopContext context, RedisService redisService, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _redisService = redisService;
            _hubContext = hubContext;
        }

        // Customer Chat Screen
        public async Task<IActionResult> CustomerChat()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Customers)
                .FirstOrDefaultAsync(u => u.Id == long.Parse(userId));

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra quyền - chỉ customer hoặc student mới được vào
            var userRoleId = user.RoleId ?? 0;
            var userRole = user.Role?.RoleName?.ToLower() ?? "";
            var isCustomer = userRoleId == 2 || userRole.Contains("customer") || userRole.Contains("student");
            
            if (!isCustomer)
            {
                // Nếu là staff, redirect sang staff chat
                if (userRoleId == 4 || userRole.Contains("staff"))
                {
                    return RedirectToAction("StaffChat", "Chat");
                }
                return RedirectToAction("Index", "Home");
            }

            // Nếu user chưa có customer record, tạo một customer record tạm hoặc redirect
            if (!user.Customers.Any())
            {
                // Có thể tạo customer record hoặc chỉ cần lấy thông tin từ user
                // Tạm thời redirect về home
                return RedirectToAction("Index", "Home");
            }

            var customer = user.Customers.First();
            ViewBag.CustomerId = customer.Id;
            ViewBag.CustomerName = $"{customer.FirstName} {customer.LastName}";

            // Load conversation history
            var conversationId = $"customer_{customer.Id}";
            var messages = await _redisService.GetMessagesAsync(conversationId);
            ViewBag.Messages = messages;

            return View();
        }

        // Staff Chat Screen
        public async Task<IActionResult> StaffChat()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var userRoleIdStr = HttpContext.Session.GetString("UserRoleId");
            var userRole = HttpContext.Session.GetString("UserRole");
            
            // Check if user is staff (RoleId = 4 or role name contains "staff")
            var isStaff = false;
            if (!string.IsNullOrEmpty(userRoleIdStr) && long.TryParse(userRoleIdStr, out var userRoleId))
            {
                isStaff = userRoleId == 4;
            }
            if (!isStaff && !string.IsNullOrEmpty(userRole))
            {
                isStaff = userRole.ToLower().Contains("staff");
            }
            
            if (string.IsNullOrEmpty(userId) || !isStaff)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.Staff)
                .FirstOrDefaultAsync(u => u.Id == long.Parse(userId));

            if (user == null || !user.Staff.Any())
            {
                return RedirectToAction("Index", "Home");
            }

            var staff = user.Staff.First();
            ViewBag.StaffId = staff.Id;
            ViewBag.StaffName = $"{staff.FirstName} {staff.LastName}";

            // Load active customers
            var activeCustomers = await _redisService.GetActiveCustomersAsync();
            ViewBag.ActiveCustomers = activeCustomers;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetConversationHistory(long customerId)
        {
            var conversationId = $"customer_{customerId}";
            var messages = await _redisService.GetMessagesAsync(conversationId);
            return Json(messages);
        }
    }
}

