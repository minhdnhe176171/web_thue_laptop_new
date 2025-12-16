using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.Services;
using web_chothue_laptop.ViewModels;

namespace web_chothue_laptop.Controllers
{
    public class ProfileController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly CloudinaryService _cloudinaryService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(Swp391LaptopContext context, CloudinaryService cloudinaryService, ILogger<ProfileController> logger)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Customers)
                .Include(u => u.Staff)
                .Include(u => u.Students)
                .Include(u => u.Managers)
                .Include(u => u.Technicals)
                .FirstOrDefaultAsync(u => u.Id == long.Parse(userId));

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin chi tiết dựa trên role
            var viewModel = new ProfileViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role?.RoleName,
                AvatarUrl = user.AvatarUrl
            };

            // Lấy thông tin từ Customer, Staff, Student, Manager, hoặc Technical
            var customer = user.Customers.FirstOrDefault();
            if (customer != null)
            {
                viewModel.FirstName = customer.FirstName;
                viewModel.LastName = customer.LastName;
                viewModel.Phone = customer.Phone;
                viewModel.Dob = customer.Dob;
            }
            else
            {
                var staff = user.Staff.FirstOrDefault();
                if (staff != null)
                {
                    viewModel.FirstName = staff.FirstName;
                    viewModel.LastName = staff.LastName;
                    viewModel.Phone = staff.Phone;
                    viewModel.Dob = staff.Dob;
                }
                else
                {
                    var student = user.Students.FirstOrDefault();
                    if (student != null)
                    {
                        viewModel.FirstName = student.FirstName;
                        viewModel.LastName = student.LastName;
                        viewModel.Phone = student.Phone;
                        viewModel.Dob = student.Dob;
                    }
                    else
                    {
                        var manager = user.Managers.FirstOrDefault();
                        if (manager != null)
                        {
                            viewModel.FirstName = manager.FirstName;
                            viewModel.LastName = manager.LastName;
                            viewModel.Phone = manager.Phone;
                            viewModel.Dob = manager.Dob;
                        }
                        else
                        {
                            var technical = user.Technicals.FirstOrDefault();
                            if (technical != null)
                            {
                                viewModel.FirstName = technical.FirstName;
                                viewModel.LastName = technical.LastName;
                                viewModel.Phone = technical.Phone;
                                viewModel.Dob = technical.Dob;
                            }
                        }
                    }
                }
            }

            return View(viewModel);
        }

        // POST: Profile/UpdateAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAvatar(IFormFile avatarFile)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại" });
            }

            if (avatarFile == null || avatarFile.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ảnh" });
            }

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)" });
            }

            try
            {
                var user = await _context.Users.FindAsync(long.Parse(userId));
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                // Xóa avatar cũ nếu có
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    await _cloudinaryService.DeleteImageAsync(user.AvatarUrl);
                }

                // Upload avatar mới lên Cloudinary
                var avatarUrl = await _cloudinaryService.UploadImageAsync(avatarFile, "avatars");

                // Lưu URL vào database
                user.AvatarUrl = avatarUrl;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật avatar thành công", avatarUrl = avatarUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating avatar");
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: Profile/CheckPhoneExists - For remote validation
        [AcceptVerbs("GET", "POST")]
        public async Task<IActionResult> CheckPhoneExists(string phone, long userId)
        {
            if (string.IsNullOrEmpty(phone))
            {
                return Json(true); // Allow empty phone
            }

            // Kiểm tra số điện thoại đã tồn tại trong các bảng (trừ user hiện tại)
            var existsInCustomer = await _context.Customers
                .AnyAsync(c => c.Phone == phone && c.CustomerId != userId && !string.IsNullOrEmpty(c.Phone));
            
            var existsInStaff = await _context.Staff
                .AnyAsync(s => s.Phone == phone && s.StaffId != userId && !string.IsNullOrEmpty(s.Phone));
            
            var existsInStudent = await _context.Students
                .AnyAsync(s => s.Phone == phone && s.StudentId != userId && !string.IsNullOrEmpty(s.Phone));
            
            var existsInManager = await _context.Managers
                .AnyAsync(m => m.Phone == phone && m.ManagerId != userId && !string.IsNullOrEmpty(m.Phone));
            
            var existsInTechnical = await _context.Technicals
                .AnyAsync(t => t.Phone == phone && t.TechnicalId != userId && !string.IsNullOrEmpty(t.Phone));

            if (existsInCustomer || existsInStaff || existsInStudent || existsInManager || existsInTechnical)
            {
                return Json("Số điện thoại này đã được sử dụng bởi tài khoản khác"); // Phone exists - return error message
            }

            return Json(true); // Phone is available
        }

        // POST: Profile/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                var userIdLong = long.Parse(userId);

                // Kiểm tra số điện thoại trùng lặp (nếu có nhập)
                if (!string.IsNullOrEmpty(model.Phone))
                {
                    // Kiểm tra số điện thoại đã tồn tại trong các bảng (trừ user hiện tại)
                    var existsInCustomer = await _context.Customers
                        .AnyAsync(c => c.Phone == model.Phone && c.CustomerId != userIdLong && !string.IsNullOrEmpty(c.Phone));
                    
                    var existsInStaff = await _context.Staff
                        .AnyAsync(s => s.Phone == model.Phone && s.StaffId != userIdLong && !string.IsNullOrEmpty(s.Phone));
                    
                    var existsInStudent = await _context.Students
                        .AnyAsync(s => s.Phone == model.Phone && s.StudentId != userIdLong && !string.IsNullOrEmpty(s.Phone));
                    
                    var existsInManager = await _context.Managers
                        .AnyAsync(m => m.Phone == model.Phone && m.ManagerId != userIdLong && !string.IsNullOrEmpty(m.Phone));
                    
                    var existsInTechnical = await _context.Technicals
                        .AnyAsync(t => t.Phone == model.Phone && t.TechnicalId != userIdLong && !string.IsNullOrEmpty(t.Phone));

                    if (existsInCustomer || existsInStaff || existsInStudent || existsInManager || existsInTechnical)
                    {
                        ModelState.AddModelError("Phone", "Số điện thoại này đã được sử dụng bởi tài khoản khác");
                        // Reload model để hiển thị lại
                        var user = await _context.Users
                            .Include(u => u.Role)
                            .Include(u => u.Customers)
                            .Include(u => u.Staff)
                            .Include(u => u.Students)
                            .Include(u => u.Managers)
                            .Include(u => u.Technicals)
                            .FirstOrDefaultAsync(u => u.Id == userIdLong);
                        
                        if (user != null)
                        {
                            model.Email = user.Email;
                            model.RoleId = user.RoleId;
                            model.RoleName = user.Role?.RoleName;
                            model.AvatarUrl = user.AvatarUrl;
                            
                            var customer1 = user.Customers.FirstOrDefault();
                            if (customer1 != null)
                            {
                                model.FirstName = customer1.FirstName;
                                model.LastName = customer1.LastName;
                                model.Phone = customer1.Phone;
                                model.Dob = customer1.Dob;
                            }
                            else
                            {
                                var staff1 = user.Staff.FirstOrDefault();
                                if (staff1 != null)
                                {
                                    model.FirstName = staff1.FirstName;
                                    model.LastName = staff1.LastName;
                                    model.Phone = staff1.Phone;
                                    model.Dob = staff1.Dob;
                                }
                                else
                                {
                                    var student1 = user.Students.FirstOrDefault();
                                    if (student1 != null)
                                    {
                                        model.FirstName = student1.FirstName;
                                        model.LastName = student1.LastName;
                                        model.Phone = student1.Phone;
                                        model.Dob = student1.Dob;
                                    }
                                    else
                                    {
                                        var manager1 = user.Managers.FirstOrDefault();

                                        if (manager1!= null)

                                        if (manager1 != null)

                                        {
                                            model.FirstName = manager1.FirstName;
                                            model.LastName = manager1.LastName;
                                            model.Phone = manager1.Phone;
                                            model.Dob = manager1.Dob;
                                        }
                                        else
                                        {
                                            var technical1 = user.Technicals.FirstOrDefault();
                                            if (technical1 != null)
                                            {
                                                model.FirstName = technical1.FirstName;
                                                model.LastName = technical1.LastName;
                                                model.Phone = technical1.Phone;
                                                model.Dob = technical1.Dob;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        return View("Index", model);
                    }
                }

                // Tìm và cập nhật Customer
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == userIdLong);
                if (customer != null)
                {
                    customer.FirstName = model.FirstName;
                    customer.LastName = model.LastName;
                    customer.Phone = model.Phone;
                    customer.Dob = model.Dob;
                    _context.Customers.Update(customer);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
                    return RedirectToAction("Index");
                }

                // Tìm và cập nhật Staff
                var staff = await _context.Staff
                    .FirstOrDefaultAsync(s => s.StaffId == userIdLong);
                if (staff != null)
                {
                    staff.FirstName = model.FirstName;
                    staff.LastName = model.LastName;
                    staff.Phone = model.Phone;
                    staff.Dob = model.Dob;
                    _context.Staff.Update(staff);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
                    return RedirectToAction("Index");
                }

                // Tìm và cập nhật Student
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == userIdLong);
                if (student != null)
                {
                    student.FirstName = model.FirstName;
                    student.LastName = model.LastName;
                    student.Phone = model.Phone;
                    student.Dob = model.Dob;
                    _context.Students.Update(student);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
                    return RedirectToAction("Index");
                }

                // Tìm và cập nhật Manager
                var manager = await _context.Managers
                    .FirstOrDefaultAsync(m => m.ManagerId == userIdLong);
                if (manager != null)
                {
                    manager.FirstName = model.FirstName;
                    manager.LastName = model.LastName;
                    manager.Phone = model.Phone;
                    manager.Dob = model.Dob;
                    _context.Managers.Update(manager);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
                    return RedirectToAction("Index");
                }

                // Tìm và cập nhật Technical
                var technical = await _context.Technicals
                    .FirstOrDefaultAsync(t => t.TechnicalId == userIdLong);
                if (technical != null)
                {
                    technical.FirstName = model.FirstName;
                    technical.LastName = model.LastName;
                    technical.Phone = model.Phone;
                    technical.Dob = model.Dob;
                    _context.Technicals.Update(technical);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công";
                    return RedirectToAction("Index");
                }

                // Nếu không tìm thấy bất kỳ record nào
                ModelState.AddModelError("", "Không tìm thấy thông tin người dùng để cập nhật");
                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông tin");
                return View("Index", model);
            }
        }
    }
}

