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
                "closed" or "đã đóng" => "bg-secondary",
                _ => "bg-secondary"
            };
        }
    }
}

