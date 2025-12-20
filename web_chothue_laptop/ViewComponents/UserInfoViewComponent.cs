using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.ViewComponents
{
    public class UserInfoViewComponent : ViewComponent
    {
        private readonly Swp391LaptopContext _context;

        public UserInfoViewComponent(Swp391LaptopContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Content(string.Empty);
            }

            var user = await _context.Users
                .Include(u => u.Customers)
                .Include(u => u.Staff)
                .Include(u => u.Students)
                .Include(u => u.Managers)
                .Include(u => u.Technicals)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == long.Parse(userId));

            if (user == null)
            {
                return Content(string.Empty);
            }

            string fullName = "User";
            string roleName = "Người dùng";
            string? avatarUrl = user.AvatarUrl;

            // Lấy role name
            if (user.Role != null)
            {
                roleName = user.Role.RoleName?.ToLower() switch
                {
                    "customer" => "Customer",
                    "student" => "Student Lessor",
                    "staff" => "Staff",
                    "manager" => "Manager",
                    "technical" => "Technical",
                    _ => user.Role.RoleName ?? "Người dùng"
                };
            }

            // Lấy tên từ Customer, Staff, Student, Manager, hoặc Technical
            var customer = user.Customers.FirstOrDefault();
            if (customer != null)
            {
                fullName = $"{customer.LastName} {customer.FirstName}";
            }
            else
            {
                var staff = user.Staff.FirstOrDefault();
                if (staff != null)
                {
                    fullName = $"{staff.LastName} {staff.FirstName}";
                }
                else
                {
                    var student = user.Students.FirstOrDefault();
                    if (student != null)
                    {
                        fullName = $"{student.LastName} {student.FirstName}";
                    }
                    else
                    {
                        var manager = user.Managers.FirstOrDefault();
                        if (manager != null)
                        {
                            fullName = $"{manager.LastName} {manager.FirstName}";
                        }
                        else
                        {
                            var technical = user.Technicals.FirstOrDefault();
                            if (technical != null)
                            {
                                fullName = $"{technical.LastName} {technical.FirstName}";
                            }
                        }
                    }
                }
            }

            ViewBag.FullName = fullName;
            ViewBag.RoleName = roleName;
            ViewBag.AvatarUrl = avatarUrl;
            ViewBag.UserEmail = user.Email;

            return View();
        }
    }
}

