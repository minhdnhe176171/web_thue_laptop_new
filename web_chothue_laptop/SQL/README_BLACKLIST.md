# H??ng d?n thêm c?t BlackList vào b?ng Customer

## B??c 1: Ch?y SQL Script

1. M? SQL Server Management Studio (SSMS)
2. K?t n?i ??n server: `HA-DONG-GIANG\MSSQLSERVER01`
3. Ch?n database: `swp391_laptop`
4. M? file `AddBlackListColumn.sql`
5. Ch?y script (F5 ho?c Execute)

Script s?:
- Thêm c?t `BLACK_LIST` (ki?u BIT, default = 0) vào b?ng CUSTOMER
- Insert 2 customer m?u:
  - Nguy?n V?n A (BlackList = false)
  - Tr?n Th? B (BlackList = true)

## B??c 2: Scaffold l?i Models

Sau khi ch?y SQL script thành công, m? Terminal/PowerShell và ch?y l?nh:

```powershell
cd web_chothue_laptop

dotnet ef dbcontext scaffold "Server=HA-DONG-GIANG\MSSQLSERVER01;Database=swp391_laptop;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true" Microsoft.EntityFrameworkCore.SqlServer -o Models -f --context Swp391LaptopContext
```

## B??c 3: Ki?m tra k?t qu?

Sau khi scaffold, file `Models/Customer.cs` s? có thêm property:

```csharp
public bool BlackList { get; set; }
```

Và file `Models/Swp391LaptopContext.cs` s? có thêm configuration:

```csharp
entity.Property(e => e.BlackList).HasColumnName("BLACK_LIST");
```

## B??c 4: Build và Test

```powershell
dotnet build
```

## S? d?ng BlackList trong Code

```csharp
// L?c customer không b? blacklist
var activeCustomers = await _context.Customers
    .Where(c => !c.BlackList)
    .ToListAsync();

// Blacklist m?t customer
var customer = await _context.Customers.FindAsync(customerId);
if (customer != null)
{
    customer.BlackList = true;
    await _context.SaveChangesAsync();
}

// Unblacklist m?t customer
customer.BlackList = false;
await _context.SaveChangesAsync();
```

## L?u ý

- C?t `BLACK_LIST` có giá tr? m?c ??nh là `0` (false) cho t?t c? customer hi?n t?i
- Khi t?o customer m?i, n?u không set giá tr? thì m?c ??nh là `false`
- Có th? thêm index cho c?t này n?u c?n query nhi?u:
  ```sql
  CREATE INDEX IX_CUSTOMER_BLACK_LIST ON CUSTOMER(BLACK_LIST);
  ```
