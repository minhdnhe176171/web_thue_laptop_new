using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Services;

public class VnpayService : IVnPayService
{
    private readonly Swp391LaptopContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VnpayService> _logger;

    public VnpayService(
        Swp391LaptopContext context,
        IConfiguration configuration,
        ILogger<VnpayService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> CreatePaymentUrlAsync(long bookingId, string ipAddress)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new Exception($"Booking {bookingId} not found");
        }

        var amount = ((long)((booking.TotalPrice ?? 0) * 100)).ToString();
        var tmnCode = _configuration["Vnpay:TmnCode"];
        var hashSecret = _configuration["Vnpay:HashSecret"];
        var baseUrl = _configuration["Vnpay:BaseUrl"];
        var returnUrl = _configuration["Vnpay:ReturnUrl"];
        var ipnUrl = _configuration["Vnpay:IpnUrl"];

        if (string.IsNullOrEmpty(tmnCode) || string.IsNullOrEmpty(hashSecret) || string.IsNullOrEmpty(baseUrl))
        {
            throw new Exception("VNPay configuration is missing");
        }

        // VNPay yêu cầu thời gian theo múi giờ Việt Nam (UTC+7)
        // Format: yyyyMMddHHmmss (ví dụ: 20241219164148)
        var createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
        var expireDate = DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss");

        // Đảm bảo OrderInfo không quá dài và không có ký tự đặc biệt
        var orderInfo = $"Thanh toan don hang {bookingId}";
        if (orderInfo.Length > 255)
        {
            orderInfo = orderInfo.Substring(0, 255);
        }

        var parameters = new SortedDictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_Amount"] = amount,
            ["vnp_CreateDate"] = createDate,
            ["vnp_CurrCode"] = "VND",
            ["vnp_ExpireDate"] = expireDate,
            ["vnp_IpAddr"] = ipAddress ?? "127.0.0.1",
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = bookingId.ToString()
        };

        // Chỉ thêm BankCode nếu có giá trị (không thêm nếu rỗng)
        // Không thêm vnp_BankCode nếu rỗng để tránh lỗi

        // Chỉ thêm IpnUrl nếu không phải localhost (VNPay không thể truy cập localhost)
        if (!string.IsNullOrEmpty(ipnUrl) && !ipnUrl.Contains("localhost") && !ipnUrl.Contains("127.0.0.1"))
        {
            parameters["vnp_IpnUrl"] = ipnUrl;
        }
        else if (!string.IsNullOrEmpty(ipnUrl) && (ipnUrl.Contains("localhost") || ipnUrl.Contains("127.0.0.1")))
        {
            _logger.LogWarning("VNPay IpnUrl is localhost - VNPay cannot access it. IPN will not work. Consider using ngrok for local development.");
        }

        // Build sign data (KHÔNG encode khi hash)
        // Loại bỏ vnp_SecureHashType khỏi sign data (nếu có)
        var signData = BuildSignData(parameters);
        var secureHash = ComputeHash(hashSecret, signData);
        
        // Build query string (CÓ encode khi tạo URL)
        var query = BuildQuery(parameters);
        
        // Thêm SecureHashType và SecureHash vào query (encode)
        var paymentUrl = $"{baseUrl}?{query}&vnp_SecureHashType=SHA256&vnp_SecureHash={WebUtility.UrlEncode(secureHash)}";
        
        // Log chi tiết để debug
        _logger.LogInformation("VNPay Payment URL created - BookingId: {BookingId}, Amount: {Amount}, CreateDate: {CreateDate}, ExpireDate: {ExpireDate}, TxnRef: {TxnRef}", 
            bookingId, amount, createDate, expireDate, bookingId.ToString());
        _logger.LogDebug("VNPay SignData: {SignData}", signData);
        _logger.LogDebug("VNPay SecureHash: {SecureHash}", secureHash);
        _logger.LogDebug("VNPay Payment URL: {PaymentUrl}", paymentUrl);
        
        // Kiểm tra và cảnh báo nếu có vấn đề với URL
        if (returnUrl.Contains("localhost") || returnUrl.Contains("127.0.0.1"))
        {
            _logger.LogWarning("VNPay ReturnUrl contains localhost - VNPay cannot access it. Payment may fail.");
        }

        return paymentUrl;
    }

    public async Task<VnPayReturnResult> ProcessReturnAsync(IQueryCollection query)
    {
        if (query == null || !query.Any())
        {
            return new VnPayReturnResult(false, string.Empty, "Không nhận được phản hồi từ VNPay.");
        }

        var responseCode = query["vnp_ResponseCode"].ToString();
        var secureHash = query["vnp_SecureHash"].ToString();
        var txnRef = query["vnp_TxnRef"].ToString();

        if (string.IsNullOrWhiteSpace(txnRef))
        {
            return new VnPayReturnResult(false, string.Empty, "Thiếu mã đơn hàng trong phản hồi.");
        }

        var parameters = query
            .Where(kvp => kvp.Key.StartsWith("vnp_") &&
                          kvp.Key != "vnp_SecureHash" &&
                          kvp.Key != "vnp_SecureHashType")
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

        var hashSecret = _configuration["Vnpay:HashSecret"];
        var signData = BuildSignData(new SortedDictionary<string, string>(parameters));
        var computedHash = ComputeHash(hashSecret, signData);

        if (!string.Equals(secureHash, computedHash, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("VNPay signature mismatch for booking {BookingId}.", txnRef);
            return new VnPayReturnResult(false, txnRef, "Chữ ký VNPay không hợp lệ.");
        }

        if (!long.TryParse(txnRef, out long bookingId))
        {
            return new VnPayReturnResult(false, txnRef, "Mã đơn hàng không hợp lệ.");
        }

        var booking = await _context.Bookings
            .Include(b => b.Status)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            return new VnPayReturnResult(false, txnRef, "Không tìm thấy đơn hàng tương ứng.");
        }

        var message = query["vnp_Message"].ToString();
        var transactionNo = query["vnp_TransactionNo"].ToString();
        var transactionStatus = query["vnp_TransactionStatus"].ToString();
        var amount = query["vnp_Amount"].ToString();

        // Xử lý các mã lỗi VNPay
        var errorMessages = new Dictionary<string, string>
        {
            ["07"] = "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
            ["09"] = "Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking.",
            ["10"] = "Xác thực thông tin thẻ/tài khoản không đúng. Quá 3 lần.",
            ["11"] = "Đã hết hạn chờ thanh toán. Xin vui lòng thực hiện lại giao dịch.",
            ["12"] = "Thẻ/Tài khoản bị khóa.",
            ["13"] = "Nhập sai mật khẩu xác thực giao dịch (OTP). Xin vui lòng thực hiện lại giao dịch.",
            ["51"] = "Tài khoản không đủ số dư để thực hiện giao dịch.",
            ["65"] = "Tài khoản đã vượt quá hạn mức giao dịch trong ngày.",
            ["75"] = "Ngân hàng thanh toán đang bảo trì.",
            ["79"] = "Nhập sai mật khẩu đăng nhập InternetBanking quá số lần quy định.",
            ["99"] = "Lỗi không xác định được. Vui lòng liên hệ VNPay để được hỗ trợ."
        };

        // Xử lý lỗi 72 - Lỗi kết nối tới hệ thống VNPay
        if (responseCode == "72")
        {
            _logger.LogWarning("VNPay error 72 for booking {BookingId}: Lỗi kết nối tới hệ thống VNPay.", bookingId);
            return new VnPayReturnResult(false, bookingId.ToString(), "Lỗi kết nối tới hệ thống VNPay. Vui lòng thử lại sau hoặc liên hệ hỗ trợ.");
        }

        if (responseCode == "00" && transactionStatus == "00")
        {
            // Kiểm tra số tiền
            if (!string.IsNullOrEmpty(amount) && long.TryParse(amount, out long vnpAmount))
            {
                long bookingAmount = (long)((booking.TotalPrice ?? 0) * 100); // VNPay trả về số tiền nhân 100
                if (bookingAmount != vnpAmount)
                {
                    _logger.LogWarning("VNPay amount mismatch for booking {BookingId}. Expected: {Expected}, Received: {Received}", 
                        bookingId, bookingAmount, vnpAmount);
                    return new VnPayReturnResult(false, bookingId.ToString(), "Số tiền thanh toán không khớp.");
                }
            }

            // Cập nhật trạng thái nếu chưa thanh toán
            if (booking.StatusId == 1 || booking.StatusId == 2) // Pending hoặc Approved
            {
                var bankedStatusId = await GetStatusIdAsync("banked");
                if (bankedStatusId.HasValue)
                {
                    booking.StatusId = bankedStatusId.Value;
                    booking.UpdatedDate = DateTime.Now;
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("VNPay Return - Successfully updated booking {BookingId} to StatusId: {StatusId}, VNPay TranId: {TranId}", 
                            bookingId, booking.StatusId, transactionNo);
                        return new VnPayReturnResult(true, bookingId.ToString(), "Thanh toán VNPay thành công.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "VNPay Return - Error saving booking {BookingId}", bookingId);
                        return new VnPayReturnResult(false, bookingId.ToString(), "Có lỗi xảy ra khi cập nhật trạng thái thanh toán.");
                    }
                }
                else
                {
                    _logger.LogError("VNPay Return - Banked status not found");
                    return new VnPayReturnResult(false, bookingId.ToString(), "Không tìm thấy trạng thái 'banked' trong hệ thống.");
                }
            }
            else
            {
                _logger.LogInformation("VNPay Return - Booking {BookingId} already processed, StatusId: {StatusId}", 
                    bookingId, booking.StatusId);
                return new VnPayReturnResult(true, bookingId.ToString(), "Đơn hàng đã được thanh toán thành công.");
            }
        }

        // Lấy thông báo lỗi chi tiết nếu có
        var errorMessage = errorMessages.TryGetValue(responseCode, out var detailedMessage) 
            ? detailedMessage 
            : (string.IsNullOrWhiteSpace(message) ? $"Thanh toán VNPay không thành công. Mã lỗi: {responseCode}" : message);

        _logger.LogWarning("VNPay payment failed for booking {BookingId}. ResponseCode: {ResponseCode}, Message: {Message}", 
            bookingId, responseCode, errorMessage);

        return new VnPayReturnResult(false, bookingId.ToString(), errorMessage);
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

        // Tạo query string để hash
        var signData = BuildSignData(new SortedDictionary<string, string>(filteredParams));
        var calculatedHash = ComputeHash(hashSecret, signData);

        var isValid = calculatedHash.Equals(vnp_SecureHash, StringComparison.InvariantCultureIgnoreCase);
        
        if (!isValid)
        {
            _logger.LogWarning("VNPay ValidateSignature - Hash mismatch. Calculated: {Calculated}, Received: {Received}", 
                calculatedHash.Substring(0, Math.Min(20, calculatedHash.Length)), 
                vnp_SecureHash.Substring(0, Math.Min(20, vnp_SecureHash.Length)));
        }

        return isValid;
    }

    private async Task<long?> GetStatusIdAsync(string statusName)
    {
        var status = await _context.Statuses.FirstOrDefaultAsync(s => s.StatusName.ToLower() == statusName.ToLower());
        return status?.Id;
    }

    private static string BuildQuery(SortedDictionary<string, string> parameters)
    {
        return string.Join("&", parameters
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(kvp => $"{kvp.Key}={WebUtility.UrlEncode(kvp.Value)}"));
    }

    private static string BuildSignData(SortedDictionary<string, string> parameters)
    {
        return string.Join("&", parameters
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }

    private static string ComputeHash(string secret, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        // VNPay yêu cầu hash phải là chữ HOA
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToUpperInvariant();
    }
}
