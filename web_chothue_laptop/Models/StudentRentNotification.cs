using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class StudentRentNotification
{
    public long Id { get; set; }

    public long StudentId { get; set; }

    public long ManagerId { get; set; }

    public long BookingId { get; set; }

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Manager Manager { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
