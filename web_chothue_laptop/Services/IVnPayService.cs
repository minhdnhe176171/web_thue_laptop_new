namespace web_chothue_laptop.Services;

public interface IVnPayService
{
    Task<string> CreatePaymentUrlAsync(long bookingId, string ipAddress);
    Task<VnPayReturnResult> ProcessReturnAsync(Microsoft.AspNetCore.Http.IQueryCollection query);
    bool ValidateSignature(Dictionary<string, string> vnp_Params, string vnp_SecureHash);
}
