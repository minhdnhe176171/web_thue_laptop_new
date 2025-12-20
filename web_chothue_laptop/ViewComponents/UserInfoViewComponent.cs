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
                .FirstOrDefaultAsync(u => u.Id == long.Parse(userId));

            if (user == null)
            {
                return Content(string.Empty);
            }

            string fullName = "User";
            string? avatarUrl = user.AvatarUrl;

            // Lấy tên từ Customer, Staff, Student, Manager, hoặc Technical
            var customer = user.Customers.FirstOrDefault();
            if (customer != null)
            {
                fullName = $"{customer.FirstName} {customer.LastName}";
            }
            else
            {
                var staff = user.Staff.FirstOrDefault();
                if (staff != null)
                {
                    fullName = $"{staff.FirstName} {staff.LastName}";
                }
                else
                {
                    var student = user.Students.FirstOrDefault();
                    if (student != null)
                    {
                        fullName = $"{student.FirstName} {student.LastName}";
                    }
                    else
                    {
                        var manager = user.Managers.FirstOrDefault();
                        if (manager != null)
                        {
                            fullName = $"{manager.FirstName} {manager.LastName}";
                        }
                        else
                        {
                            var technical = user.Technicals.FirstOrDefault();
                            if (technical != null)
                            {
                                fullName = $"{technical.FirstName} {technical.LastName}";
                            }
                        }
                    }
                }
            }

            ViewBag.FullName = fullName;
            ViewBag.AvatarUrl = avatarUrl;
            ViewBag.UserEmail = user.Email;

            return View();
        }
    }
}

