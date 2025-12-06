using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class Staff
{
    public long Id { get; set; }

    public long? StaffId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? IdNo { get; set; }

    public DateTime? Dob { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<BookingReceipt> BookingReceipts { get; set; } = new List<BookingReceipt>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual User? StaffNavigation { get; set; }

    public virtual ICollection<TechnicalTicket> TechnicalTickets { get; set; } = new List<TechnicalTicket>();

    public virtual ICollection<TicketList> TicketLists { get; set; } = new List<TicketList>();
}
