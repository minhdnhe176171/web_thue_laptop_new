using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class User
{
    public long Id { get; set; }

    public long? RoleId { get; set; }

    public long? StatusId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public string? OtpCode { get; set; }

    public DateTime? OtpExpiry { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    public virtual ICollection<Manager> Managers { get; set; } = new List<Manager>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual Status? Status { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Technical> Technicals { get; set; } = new List<Technical>();
}
