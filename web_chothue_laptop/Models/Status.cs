using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class Status
{
    public long Id { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Laptop> Laptops { get; set; } = new List<Laptop>();

    public virtual ICollection<TechnicalTicket> TechnicalTickets { get; set; } = new List<TechnicalTicket>();

    public virtual ICollection<Technical> Technicals { get; set; } = new List<Technical>();

    public virtual ICollection<TicketList> TicketLists { get; set; } = new List<TicketList>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
