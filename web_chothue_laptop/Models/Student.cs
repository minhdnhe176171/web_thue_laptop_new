using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class Student
{
    public long Id { get; set; }

    public long? StudentId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? IdNo { get; set; }

    public DateTime? Dob { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<Laptop> Laptops { get; set; } = new List<Laptop>();

    public virtual User? StudentNavigation { get; set; }

    public virtual ICollection<StudentRentNotification> StudentRentNotifications { get; set; } = new List<StudentRentNotification>();
}
