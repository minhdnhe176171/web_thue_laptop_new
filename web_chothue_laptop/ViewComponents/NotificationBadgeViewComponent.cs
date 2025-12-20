using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.ViewComponents
{
    public class NotificationBadgeViewComponent : ViewComponent
    {
        private readonly Swp391LaptopContext _context;

        public NotificationBadgeViewComponent(Swp391LaptopContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return View(0);
            }

            var userIdLong = long.Parse(userId);
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == userIdLong);

            if (student == null)
            {
                return View(0);
            }

            // ??m s? l??ng th�ng b�o ch?a x�c nh?n (t?m th?i tr? v? 0 v� RejectReason kh�ng c� trong model)
            var unconfirmedCount = 0;
            // var unconfirmedCount = await _context.Bookings
            //     .Where(b => b.Laptop.StudentId == student.Id &&
            //                b.StatusId == 8 &&
            //                !string.IsNullOrEmpty(b.RejectReason) &&
            //                b.RejectReason.StartsWith("RETURN_SCHEDULE|") &&
            //                !b.RejectReason.Contains("|CONFIRMED|"))
            //     .CountAsync();

            return View(unconfirmedCount);
        }
    }
}
