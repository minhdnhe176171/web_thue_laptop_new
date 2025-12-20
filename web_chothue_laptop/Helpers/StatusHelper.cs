namespace web_chothue_laptop.Helpers
{
    public static class StatusHelper
    {
        public static string GetVietnameseStatus(string? statusName)
        {
            if (string.IsNullOrEmpty(statusName))
                return "Chưa xác định";

            var status = statusName.ToLower().Trim();

            return status switch
            {
                "available" => "Có sẵn",
                "successfully" or "success" => "Hoàn thành",
                "completed" => "Hoàn thành",
                "rented" or "renting" => "Đang thuê",
                "unavailable" => "Không có sẵn",
                "pending" => "Đang chờ",
                "cancelled" or "canceled" => "Đã hủy",
                "active" => "Đang hoạt động",
                "inactive" => "Không hoạt động",
                "approved" => "Đã Duyệt",
                "banked" => "Đã chuyển khoản",
                "closed" => "Đã đóng",
                _ => statusName // Giữ nguyên nếu không tìm thấy mapping
            };
        }

        public static string GetStatusBadgeClass(string? statusName)
        {
            if (string.IsNullOrEmpty(statusName))
                return "bg-secondary";

            var status = statusName.ToLower().Trim();

            return status switch
            {
                "available" or "có sẵn" => "bg-success",
                "successfully" or "success" or "completed" or "hoàn thành" => "bg-info",
                "rented" or "renting" or "đang thuê" => "bg-warning",
                "unavailable" or "không có sẵn" => "bg-danger",
                "pending" or "đang chờ" => "bg-warning",
                "cancelled" or "canceled" or "đã hủy" => "bg-secondary",
                "approved" or "đã duyệt" => "bg-success",
                "banked" or "đã chuyển khoản" => "bg-success",
                "closed" or "đã đóng" or "đã hoàn thành" => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        // Method để hiển thị status trong lịch sử sử dụng và theo dõi đơn thuê
        // "closed" hoặc "close" sẽ hiển thị là "Đã hoàn thành" thay vì "Đã đóng"
        // "banked" sẽ hiển thị là "Đã chuyển khoản"
        public static string GetVietnameseStatusForHistory(string? statusName)
        {
            if (string.IsNullOrEmpty(statusName))
                return "Chưa xác định";

            // Xử lý trim và toLower để đảm bảo không có khoảng trắng và không phân biệt hoa thường
            var status = statusName.Trim().ToLower();

            // Xử lý "close" hoặc "closed" trước (có thể database lưu là "Close", "CLOSE", "closed", "close", v.v.)
            if (status == "close" || status == "closed" || status.Contains("close"))
                return "Đã hoàn thành";

            // Xử lý các status đặc biệt cho lịch sử và theo dõi đơn thuê
            return status switch
            {
                "available" => "Có sẵn",
                "successfully" or "success" => "Hoàn thành",
                "completed" => "Hoàn thành",
                "rented" or "renting" => "Đang thuê",
                "unavailable" => "Không có sẵn",
                "pending" => "Đang chờ",
                "cancelled" or "canceled" => "Đã hủy",
                "active" => "Đang hoạt động",
                "inactive" => "Không hoạt động",
                "approved" => "Đã Duyệt",
                "banked" => "Đã chuyển khoản", // Banked -> Đã chuyển khoản
                _ => GetVietnameseStatus(statusName) // Nếu không tìm thấy, dùng method gốc
            };
        }

        // Method để lấy màu badge cho status trong lịch sử sử dụng và theo dõi đơn thuê
        // "closed"/"close" và "banked" cần màu phù hợp khi hiển thị trong lịch sử
        public static string GetStatusBadgeClassForHistory(string? statusName)
        {
            if (string.IsNullOrEmpty(statusName))
                return "bg-secondary";

            // Xử lý trim và toLower để đảm bảo không có khoảng trắng và không phân biệt hoa thường
            var status = statusName.Trim().ToLower();

            // Xử lý "close" hoặc "closed" trước - dùng màu bg-info cho "Đã hoàn thành"
            if (status == "close" || status == "closed" || status.Contains("close"))
                return "bg-info";

            return status switch
            {
                "available" or "có sẵn" => "bg-success",
                "successfully" or "success" or "completed" or "hoàn thành" => "bg-info",
                "rented" or "renting" or "đang thuê" => "bg-warning",
                "unavailable" or "không có sẵn" => "bg-danger",
                "pending" or "đang chờ" => "bg-warning",
                "cancelled" or "canceled" or "đã hủy" => "bg-secondary",
                "approved" or "đã duyệt" => "bg-success",
                "banked" => "bg-success", // "banked" hiển thị là "Đã chuyển khoản" nên dùng bg-success
                "đã hoàn thành" => "bg-info", // Đảm bảo "Đã hoàn thành" có màu bg-info
                "đã chuyển khoản" => "bg-success", // Đảm bảo "Đã chuyển khoản" có màu bg-success
                _ => GetStatusBadgeClass(statusName) // Nếu không tìm thấy, dùng method gốc
            };
        }
    }
}







