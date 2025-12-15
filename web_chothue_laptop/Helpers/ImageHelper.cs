namespace web_chothue_laptop.Helpers
{
    public static class ImageHelper
    {
        public static string GetLaptopImageUrl(string? imageUrl, string? laptopName, string? brandName)
        {
            // Ưu tiên dùng URL từ database (Cloudinary)
            if (!string.IsNullOrEmpty(imageUrl))
                return imageUrl;

            // Fallback: dùng placeholder nếu chưa có URL
            if (string.IsNullOrEmpty(laptopName))
                return "https://via.placeholder.com/400x200?text=Laptop";

            var name = laptopName.ToLower();
            var brand = brandName?.ToLower() ?? "";

            // Map tên laptop với ảnh placeholder
            if (name.Contains("dell") || name.Contains("inspiron"))
                return "https://via.placeholder.com/400x200?text=Dell+Inspiron+14";
            else if (name.Contains("lenovo") || name.Contains("ideapad"))
                return "https://via.placeholder.com/400x200?text=Lenovo+IdeaPad+Gaming+3";
            else if (name.Contains("asus") || name.Contains("tuf"))
                return "https://via.placeholder.com/400x200?text=Asus+TUF+Gaming+F15";
            else if (name.Contains("acer") || name.Contains("nitro"))
                return "https://via.placeholder.com/400x200?text=Acer+Nitro+5";
            else if (name.Contains("msi") || name.Contains("bravo"))
                return "https://via.placeholder.com/400x200?text=MSI+Bravo+15";
            else if (name.Contains("hp") || name.Contains("victus"))
                return "https://via.placeholder.com/400x200?text=HP+Victus+15";

            return $"https://via.placeholder.com/400x200?text={Uri.EscapeDataString(laptopName)}";
        }
    }
}



