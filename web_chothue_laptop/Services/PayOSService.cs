using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Net.payOS;
using Net.payOS.Types;
using web_chothue_laptop.Models;

namespace web_chothue_laptop.Services;

public class PayOSService
{
    private readonly Swp391LaptopContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayOSService> _logger;
    private readonly Net.payOS.PayOS _payOS;

    public PayOSService(
        Swp391LaptopContext context,
        IConfiguration configuration,
        ILogger<PayOSService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        
        var clientId = configuration["PayOS:ClientId"] ?? throw new Exception("PayOS:ClientId is missing");
        var apiKey = configuration["PayOS:ApiKey"] ?? throw new Exception("PayOS:ApiKey is missing");
        var checksumKey = configuration["PayOS:ChecksumKey"] ?? throw new Exception("PayOS:ChecksumKey is missing");
        
        // Log để kiểm tra (không log full key vì bảo mật)
        logger.LogDebug("PayOS Service initialized - ClientId: {ClientId}, ApiKey: {ApiKeyPrefix}..., ChecksumKey length: {ChecksumKeyLength}",
            clientId, apiKey.Substring(0, Math.Min(10, apiKey.Length)), checksumKey.Length);
        
        _payOS = new Net.payOS.PayOS(clientId, apiKey, checksumKey);
    }

    public async Task<string> CreatePaymentLinkAsync(long bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Laptop)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
        {
            throw new Exception($"Booking {bookingId} not found");
        }

        var returnUrl = _configuration["PayOS:ReturnUrl"];
        var cancelUrl = _configuration["PayOS:CancelUrl"];

        var amount = (int)(booking.TotalPrice ?? 0); // PayOS 1.0.8 sử dụng int cho amount
        
        // Tạo orderCode unique bằng cách thêm timestamp (giây)
        // Format: bookingId + timestamp (10 chữ số cuối)
        // Ví dụ: bookingId=26, timestamp=1234567890 => orderCode = 261234567890
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var orderCode = int.Parse($"{bookingId}{timestamp.ToString().Substring(timestamp.ToString().Length - 6)}");
        
        var description = $"Thanh toan don hang {bookingId}";
        
        var items = new List<ItemData>
        {
            new ItemData(
                $"Thue laptop {booking.Laptop?.Name ?? "Laptop"}",
                1,
                amount
            )
        };

        // Đảm bảo returnUrl và cancelUrl không null, thêm orderCode vào return URL
        var finalReturnUrl = returnUrl ?? $"{_configuration["PayOS:BaseUrl"]}/Booking/PayOSReturn?bookingId={bookingId}&orderCode={orderCode}";
        var finalCancelUrl = cancelUrl ?? $"{_configuration["PayOS:BaseUrl"]}/Booking/OnlinePayment?bookingId={bookingId}";

        var paymentData = new PaymentData(
            orderCode,
            amount,
            description,
            items,
            finalReturnUrl,
            finalCancelUrl
        );

        try
        {
            _logger.LogInformation("PayOS - Creating payment link for BookingId: {BookingId}, OrderCode: {OrderCode}, Amount: {Amount}, ReturnUrl: {ReturnUrl}, CancelUrl: {CancelUrl}",
                bookingId, orderCode, amount, finalReturnUrl, finalCancelUrl);
            
            // Log payment data để debug
            _logger.LogDebug("PayOS PaymentData - OrderCode: {OrderCode}, Amount: {Amount}, Description: {Description}, ItemsCount: {ItemsCount}",
                paymentData.orderCode, paymentData.amount, paymentData.description, paymentData.items?.Count ?? 0);
            
            var result = await _payOS.createPaymentLink(paymentData);
            
            _logger.LogInformation("PayOS Payment Link created - BookingId: {BookingId}, OrderCode: {OrderCode}, CheckoutUrl: {CheckoutUrl}",
                bookingId, orderCode, result.checkoutUrl);
            
            return result.checkoutUrl;
        }
        catch (Net.payOS.Errors.PayOSError payOSError)
        {
            _logger.LogError("PayOS Payment Link creation failed - BookingId: {BookingId}, OrderCode: {OrderCode}, PayOSError Message: {Message}",
                bookingId, orderCode, payOSError.Message);
            throw new Exception($"PayOS API error: {payOSError.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS Payment Link creation failed - BookingId: {BookingId}, OrderCode: {OrderCode}", bookingId, orderCode);
            throw;
        }
    }
    
    public WebhookData VerifyWebhookData(WebhookType webhookBody)
    {
        try
        {
            return _payOS.verifyPaymentWebhookData(webhookBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS Webhook verification failed");
            throw;
        }
    }
}
