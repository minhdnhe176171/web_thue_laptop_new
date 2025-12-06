using System;
using System.Collections.Generic;

namespace web_chothue_laptop.Models;

public partial class LaptopDetail
{
    public long Id { get; set; }

    public long LaptopId { get; set; }

    public string? Gpu { get; set; }

    public string? RamSize { get; set; }

    public string? RamType { get; set; }

    public string? Storage { get; set; }

    public string? ScreenSize { get; set; }

    public string? Os { get; set; }

    public string? Cpu { get; set; }

    public virtual Laptop Laptop { get; set; } = null!;
}
