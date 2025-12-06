using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class TechnicalTicket
{
    public long Id { get; set; }

    public long LaptopId { get; set; }

    public long? BookingId { get; set; }

    public long StaffId { get; set; }

    public long? TechnicalId { get; set; }

    public string Description { get; set; } = null!;

    public string? TechnicalResponse { get; set; }

    public long StatusId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual Laptop Laptop { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual Technical? Technical { get; set; }

    public virtual ICollection<TicketList> TicketLists { get; set; } = new List<TicketList>();
}
