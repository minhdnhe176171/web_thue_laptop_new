using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class TicketList
{
    public long Id { get; set; }

    public long CustomerId { get; set; }

    public long StaffId { get; set; }

    public long LaptopId { get; set; }

    public long TechnicalTicketId { get; set; }

    public decimal FixedCost { get; set; }

    public string Description { get; set; } = null!;

    public string? CustomerResponse { get; set; }

    public long StatusId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Laptop Laptop { get; set; } = null!;

    public virtual Staff Staff { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual TechnicalTicket TechnicalTicket { get; set; } = null!;
}
