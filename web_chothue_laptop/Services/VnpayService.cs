using System.Security.Cryptography;
using System.Text;
using System.Net;
using QRCoder;

namespace web_chothue_laptop.Services
{
    public class VnpayService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VnpayService> _logger;

        public VnpayService(IConfiguration configuration, ILogger<VnpayService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string CreatePaymentUrl(long bookingId, decimal amount, string orderInfo, Microsoft.AspNetCore.Http.HttpContext? httpContext = null)
        {
            var tmnCode = _configuration["Vnpay:TmnCode"];
            var hashSecret = _configuration["Vnpay:HashSecret"];
            var baseUrl = _configuration["Vnpay:BaseUrl"];
            var returnUrl = _configuration["Vnpay:ReturnUrl"];
            var ipnUrl = _configuration["Vnpay:IpnUrl"];

            if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret) || string.IsNullOrEmpty(baseUrl))
            {
                throw new Exception("VNPay configuration is missing");
            }

            // Tạo ReturnUrl động từ request hiện tại nếu có HttpContext
            if (httpContext != null && !string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("localhost"))
            {
                var request = httpContext.Request;
                var scheme = request.Scheme;
                var host = request.Host.Value;
                var pathBase = request.PathBase.Value;
                returnUrl = $"{scheme}://{host}{pathBase}/Booking/VnpayReturn";
                
                if (!string.IsNullOrEmpty(ipnUrl) && ipnUrl.Contains("localhost"))
                {
                    ipnUrl = $"{scheme}://{host}{pathBase}/Booking/VnpayIpn";
                }
            }

            // Tạo vnp_Params
            var vnp_Params = new SortedDictionary<string, string>();
            vnp_Params.Add("vnp_Version", "2.1.0");
            vnp_Params.Add("vnp_Command", "pay");
            vnp_Params.Add("vnp_TmnCode", tmnCode);
            vnp_Params.Add("vnp_Amount", ((long)(amount * 100)).ToString()); // VNPay yêu cầu số tiền nhân 100
            vnp_Params.Add("vnp_CurrCode", "VND");
            vnp_Params.Add("vnp_TxnRef", bookingId.ToString());
            vnp_Params.Add("vnp_OrderInfo", orderInfo);
            vnp_Params.Add("vnp_OrderType", "other");
            vnp_Params.Add("vnp_Locale", "vn");
            vnp_Params.Add("vnp_ReturnUrl", returnUrl);
            vnp_Params.Add("vnp_IpAddr", GetIpAddress(httpContext));
            vnp_Params.Add("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            // Thêm thời gian hết hạn (15 phút từ thời điểm tạo)
            vnp_Params.Add("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));
            
            // Thêm IPN URL nếu có
            if (!string.IsNullOrEmpty(ipnUrl))
            {
                vnp_Params.Add("vnp_IpnUrl", ipnUrl);
            }

            // Tạo query string để hash (KHÔNG encode)
            var queryString = string.Join("&", vnp_Params.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            // Tạo vnp_SecureHash
            var vnp_SecureHash = HmacSHA512(hashSecret, queryString);
            vnp_Params.Add("vnp_SecureHash", vnp_SecureHash);

            // Tạo URL thanh toán (encode khi tạo URL)
            var paymentUrl = baseUrl + "?" + string.Join("&", vnp_Params.Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));

            // Log để debug
            _logger.LogInformation("VNPay Payment URL created - BookingId: {BookingId}, Amount: {Amount}, ReturnUrl: {ReturnUrl}, PaymentUrl: {PaymentUrl}", 
                bookingId, amount, returnUrl, paymentUrl);

            return paymentUrl;
        }

        public bool ValidateSignature(Dictionary<string, string> vnp_Params, string vnp_SecureHash)
        {
            var hashSecret = _configuration["Vnpay:HashSecret"];
            if (string.IsNullOrEmpty(hashSecret))
            {
                _logger.LogError("VNPay HashSecret is missing");
                return false;
            }

            if (string.IsNullOrEmpty(vnp_SecureHash))
            {
                _logger.LogError("VNPay vnp_SecureHash is empty");
                return false;
            }

            // Loại bỏ vnp_SecureHash và vnp_SecureHashType khỏi params
            var filteredParams = vnp_Params
                .Where(kvp => kvp.Key.StartsWith("vnp_") && kvp.Key != "vnp_SecureHash" && kvp.Key != "vnp_SecureHashType")
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Tạo query string (không encode khi tạo hash, giữ nguyên giá trị)
            var queryString = string.Join("&", filteredParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            // Log để debug
            _logger.LogDebug("VNPay ValidateSignature - QueryString: {QueryString}", queryString);

            // Tạo hash
            var calculatedHash = HmacSHA512(hashSecret, queryString);

            var isValid = calculatedHash.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
            
            if (!isValid)
            {
                _logger.LogWarning("VNPay ValidateSignature - Hash mismatch. Calculated: {Calculated}, Received: {Received}", 
                    calculatedHash.Substring(0, Math.Min(20, calculatedHash.Length)), 
                    vnp_SecureHash.Substring(0, Math.Min(20, vnp_SecureHash.Length)));
            }

            return isValid;
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        public string GetIpAddress(Microsoft.AspNetCore.Http.HttpContext? httpContext = null)
        {
            if (httpContext != null)
            {
                // Lấy IP từ HttpContext
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    return ipAddress;
                }
            }
            return "127.0.0.1";
        }

        /// <summary>
        /// Tạo QR code thanh toán VietQR với thông tin tài khoản MBBank
        /// </summary>
        /// <param name="amount">Số tiền cần thanh toán</param>
        /// <param name="note">Nội dung thanh toán (thường là mã đơn hàng)</param>
        /// <returns>URL của QR code VietQR</returns>
        public string CreatePaymentQrCode(decimal amount, string note = "")
        {
            // Lấy thông tin từ appsettings.json
            var bankCode = _configuration["BankTransfer:BankCode"] ?? "MB"; // MBBank
            var accountNumber = _configuration["BankTransfer:AccountNumber"] ?? "0862735289";
            var accountName = _configuration["BankTransfer:AccountName"] ?? "Ha Hoang Hiep";
            
            // Format VietQR API: https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.jpg?amount={amount}&addInfo={note}
            // Bank code cho MBBank là "MB"
            var qrCodeUrl = $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.jpg?amount={amount}&addInfo={WebUtility.UrlEncode(note)}";
            
            _logger.LogInformation("VietQR Code created - Amount: {Amount}, Account: {AccountName} ({AccountNumber}), BankCode: {BankCode}, Note: {Note}", 
                amount, accountName, accountNumber, bankCode, note);

            return qrCodeUrl;
        }

        /// <summary>
        /// Tạo QR code VietQR từ thông tin tài khoản (dùng cho chuyển khoản trực tiếp)
        /// </summary>
        /// <param name="amount">Số tiền cần thanh toán</param>
        /// <param name="bookingId">Mã đơn hàng (dùng làm nội dung chuyển khoản)</param>
        /// <returns>Base64 string của QR code image hoặc URL</returns>
        public string CreateVietQrCode(decimal amount, long bookingId)
        {
            var bankCode = _configuration["BankTransfer:BankCode"] ?? "MB";
            var accountNumber = _configuration["BankTransfer:AccountNumber"] ?? "0862735289";
            var accountName = _configuration["BankTransfer:AccountName"] ?? "Ha Hoang Hiep";
            
            // Tạo nội dung chuyển khoản với mã đơn hàng
            string note = $"THUE LAPTOP #{bookingId}";
            
            // Tạo URL VietQR
            var vietQrUrl = $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact2.jpg?amount={amount}&addInfo={WebUtility.UrlEncode(note)}";
            
            _logger.LogInformation("VietQR Code created for direct transfer - Amount: {Amount}, Account: {AccountName} ({AccountNumber}), BankCode: {BankCode}, BookingId: {BookingId}", 
                amount, accountName, accountNumber, bankCode, bookingId);

            // Có thể tạo QR code từ URL này bằng QRCoder hoặc trả về URL trực tiếp
            // Ở đây trả về URL để frontend hiển thị, hoặc có thể tạo base64 từ URL này
            return vietQrUrl;
        }

        /// <summary>
        /// Tạo QR code từ VNPay payment URL bằng QRCoder
        /// </summary>
        /// <param name="paymentUrl">VNPay payment URL</param>
        /// <returns>Base64 string của QR code image</returns>
        public string CreateQrCodeFromUrl(string paymentUrl)
        {
            try
            {
                _logger.LogInformation("Creating QR code from VNPay payment URL - URL length: {Length}", paymentUrl?.Length ?? 0);
                
                if (string.IsNullOrEmpty(paymentUrl))
                {
                    _logger.LogError("Payment URL is null or empty");
                    throw new ArgumentException("Payment URL cannot be null or empty");
                }

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    // Sử dụng ECCLevel.H (High) để đảm bảo QR code có thể quét được ngay cả khi bị mờ một phần
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(paymentUrl, QRCodeGenerator.ECCLevel.H);
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        // Tăng kích thước QR code lên 25 pixels per module để dễ quét hơn (màu mặc định: đen trên nền trắng)
                        byte[] qrCodeBytes = qrCode.GetGraphic(25);
                        string base64String = Convert.ToBase64String(qrCodeBytes);
                        string result = $"data:image/png;base64,{base64String}";
                        
                        _logger.LogInformation("QR code created successfully - Base64 length: {Length}, ECC Level: H, Size: 25px/module", base64String?.Length ?? 0);
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating QR code from URL - PaymentUrl: {PaymentUrl}", paymentUrl);
                // Fallback: dùng external API nếu QRCoder lỗi
                var fallbackUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&ecc=H&data={WebUtility.UrlEncode(paymentUrl)}";
                _logger.LogWarning("Using fallback QR code API: {FallbackUrl}", fallbackUrl);
                return fallbackUrl;
            }
        }
    }
}
