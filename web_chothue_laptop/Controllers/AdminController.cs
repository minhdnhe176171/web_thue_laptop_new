using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.Services;

namespace web_chothue_laptop.Controllers
{
    public class AdminController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly CloudinaryService _cloudinaryService;

        public AdminController(Swp391LaptopContext context, ILogger<AdminController> logger, CloudinaryService cloudinaryService)
        {
            _context = context;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
        }

        // GET: Admin/UploadImages
        public async Task<IActionResult> UploadImages()
        {
            var laptops = await _context.Laptops
                .Include(l => l.Brand)
                .OrderByDescending(l => l.CreatedDate) // Sắp xếp theo ngày tạo mới nhất
                .ThenBy(l => l.Name)
                .ToListAsync();

            return View(laptops);
        }

        // POST: Admin/UploadMultipleImages
        [HttpPost]
        public async Task<IActionResult> UploadMultipleImages(List<IFormFile> imageFiles, List<long> laptopIds)
        {
            if (imageFiles == null || imageFiles.Count == 0 || laptopIds == null || laptopIds.Count == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn file ảnh và laptop" });
            }

            if (imageFiles.Count != laptopIds.Count)
            {
                return Json(new { success = false, message = "Số lượng ảnh và laptop không khớp" });
            }

            var results = new List<object>();
            var successCount = 0;
            var failCount = 0;

            for (int i = 0; i < imageFiles.Count; i++)
            {
                try
                {
                    var laptop = await _context.Laptops.FindAsync(laptopIds[i]);
                    if (laptop == null)
                    {
                        results.Add(new { laptopId = laptopIds[i], success = false, message = "Không tìm thấy laptop" });
                        failCount++;
                        continue;
                    }

                    // Upload ảnh lên Cloudinary
                    var imageUrl = await _cloudinaryService.UploadImageAsync(imageFiles[i], "laptops");

                    // Lưu URL vào database
                    laptop.ImageUrl = imageUrl;
                    laptop.UpdatedDate = DateTime.Now;

                    await _context.SaveChangesAsync();

                    results.Add(new { laptopId = laptopIds[i], laptopName = laptop.Name, success = true, imageUrl = imageUrl });
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error uploading image for laptop {laptopIds[i]}");
                    results.Add(new { laptopId = laptopIds[i], success = false, message = ex.Message });
                    failCount++;
                }
            }

            return Json(new
            {
                success = true,
                message = $"Upload hoàn tất: {successCount} thành công, {failCount} thất bại",
                results = results
            });
        }
    }
}