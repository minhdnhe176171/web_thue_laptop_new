using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class Customer
{
    public long Id { get; set; }

    public long? CustomerId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? IdNo { get; set; }

    public DateTime? Dob { get; set; }

    public DateTime? CreatedDate { get; set; }

    public bool BlackList { get; set; }

    public virtual ICollection<BookingReceipt> BookingReceipts { get; set; } = new List<BookingReceipt>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual User? CustomerNavigation { get; set; }

    public virtual ICollection<TicketList> TicketLists { get; set; } = new List<TicketList>();
}
