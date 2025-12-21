namespace web_chothue_laptop.ViewModels
{
    public class PaymentConfirmationViewModel
    {
        public int BookingId { get; set; }
        public string? LaptopName { get; set; }
        public string? LaptopImageUrl { get; set; } 
        public decimal TotalAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? CustomerName { get; set; }
    }
}