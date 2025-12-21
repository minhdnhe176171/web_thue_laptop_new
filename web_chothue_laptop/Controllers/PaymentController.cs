using Microsoft.AspNetCore.Mvc;
using web_chothue_laptop.Models;
using web_chothue_laptop.Helpers;

namespace web_chothue_laptop.Controllers
{
    public class PaymentController : Controller
    {
        private readonly Swp391LaptopContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(Swp391LaptopContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Action tạo URL thanh toán (POST từ View Payment)
        [HttpPost]
        public IActionResult CreatePaymentUrl(long bookingId)
        {
            var booking = _context.Bookings.Find(bookingId);
            if (booking == null) return NotFound();

            // Lấy cấu hình từ appsettings.json
            string vnp_Returnurl = _configuration["VnPay:PaymentBackReturnUrl"];
            string vnp_Url = _configuration["VnPay:BaseUrl"];
            string vnp_TmnCode = _configuration["VnPay:TmnCode"];
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];

            VnPayLibrary vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            // Số tiền thanh toán (nhân 100)
            long amount = (long)((booking.TotalPrice ?? 0) * 100);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());

            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(HttpContext));
            vnpay.AddRequestData("vnp_Locale", "vn");

            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang #" + booking.Id);
            vnpay.AddRequestData("vnp_OrderType", "other");

            // Mã tham chiếu giao dịch: BookingId_Ticks
            vnpay.AddRequestData("vnp_TxnRef", booking.Id.ToString() + "_" + DateTime.Now.Ticks.ToString());

            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return Redirect(paymentUrl);
        }

        // Action nhận kết quả trả về từ VNPay (Callback)
        // Action nhận kết quả trả về từ VNPay (Callback)
        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            if (Request.Query.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy Secret Key
            string vnp_HashSecret = _configuration["VnPay:HashSecret"];
            var vnpayData = Request.Query;
            VnPayLibrary vnpay = new VnPayLibrary();

            // Duyệt qua các tham số trả về để kiểm tra chữ ký
            foreach (var s in vnpayData)
            {
                if (!string.IsNullOrEmpty(s.Key) && s.Key.StartsWith("vnp_"))
                {
                    vnpay.AddRequestData(s.Key, s.Value);
                }
            }

            // Lấy các tham số quan trọng
            string vnp_SecureHash = vnpayData["vnp_SecureHash"];
            long vnp_Amount = Convert.ToInt64(vnpayData["vnp_Amount"]) / 100;
            string vnp_ResponseCode = vnpayData["vnp_ResponseCode"];
            string vnp_TransactionNo = vnpayData["vnp_TransactionNo"];
            string vnp_TxnRef = vnpayData["vnp_TxnRef"];

            // Kiểm tra chữ ký
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (checkSignature)
            {
                if (vnp_ResponseCode == "00") // Thanh toán thành công
                {
                    long bookingId = Convert.ToInt64(vnp_TxnRef.Split('_')[0]);

                    var booking = _context.Bookings.Find(bookingId);
                    if (booking != null)
                    {
                        // --- SỬA ĐỔI TẠI ĐÂY: Cập nhật thành StatusId = 12 (Banked) ---
                        if (booking.StatusId != 12)
                        {
                            booking.StatusId = 12; // 12 là trạng thái Banked / Đã thanh toán
                            booking.UpdatedDate = DateTime.Now;
                            _context.SaveChanges();
                        }

                        ViewBag.Message = "Thanh toán thành công";
                        ViewBag.TransactionId = vnp_TransactionNo;
                        ViewBag.Amount = vnp_Amount;
                        ViewBag.BookingId = bookingId;

                        return View("~/Views/Booking/PaymentSuccess.cshtml");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = $"Thanh toán thất bại hoặc bị hủy. Mã lỗi VNPay: {vnp_ResponseCode}";
                    return RedirectToAction("MyBookings", "Booking");
                }
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình xử lý (Sai chữ ký số).";
            return RedirectToAction("MyBookings", "Booking");
        }
    }
}