using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class Laptop
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public long? BrandId { get; set; }

    public decimal? Price { get; set; }

    public long? StatusId { get; set; }

    public long? ManagerId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public long? StudentId { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? EndTime { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Brand? Brand { get; set; }

    public virtual ICollection<LaptopDetail> LaptopDetails { get; set; } = new List<LaptopDetail>();

    public virtual Manager? Manager { get; set; }

    public virtual Status? Status { get; set; }

    public virtual Student? Student { get; set; }

    public virtual ICollection<TechnicalTicket> TechnicalTickets { get; set; } = new List<TechnicalTicket>();

    public virtual ICollection<TicketList> TicketLists { get; set; } = new List<TicketList>();
}
