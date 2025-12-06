using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class Brand
{
    public long Id { get; set; }

    public string BrandName { get; set; } = null!;

    public virtual ICollection<Laptop> Laptops { get; set; } = new List<Laptop>();
}
