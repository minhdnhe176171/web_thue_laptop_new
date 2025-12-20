using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class Booking
{
    public long Id { get; set; }

    public long CustomerId { get; set; }

    public long LaptopId { get; set; }

    public long? StaffId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public decimal? TotalPrice { get; set; }

    public long StatusId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? RejectReason { get; set; }

    public string? IdNoUrl { get; set; }

    public string? StudentUrl { get; set; }

    public DateTime? ReturnDueDate { get; set; }

    public virtual ICollection<BookingReceipt> BookingReceipts { get; set; } = new List<BookingReceipt>();

    public virtual Customer Customer { get; set; } = null!;

    public virtual Laptop Laptop { get; set; } = null!;

    public virtual Staff? Staff { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<StudentRentNotification> StudentRentNotifications { get; set; } = new List<StudentRentNotification>();

    public virtual ICollection<TechnicalTicket> TechnicalTickets { get; set; } = new List<TechnicalTicket>();
}
