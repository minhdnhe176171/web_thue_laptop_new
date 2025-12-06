using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class BookingReceipt
{
    public long Id { get; set; }

    public long BookingId { get; set; }

    public long CustomerId { get; set; }

    public long StaffId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int? LateMinutes { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal? LateFee { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;
}
