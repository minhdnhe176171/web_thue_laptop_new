using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class Technical
{
    public long Id { get; set; }

    public long? TechnicalId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? IdNo { get; set; }

    public DateTime? Dob { get; set; }

    public long? IsWorking { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Status? IsWorkingNavigation { get; set; }

    public virtual User? TechnicalNavigation { get; set; }

    public virtual ICollection<TechnicalTicket> TechnicalTickets { get; set; } = new List<TechnicalTicket>();
}
