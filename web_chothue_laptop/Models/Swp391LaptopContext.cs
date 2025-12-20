using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace web_chothue_laptop.Models;

public partial class Swp391LaptopContext : DbContext
{
    public Swp391LaptopContext()
    {
    }

    public Swp391LaptopContext(DbContextOptions<Swp391LaptopContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingReceipt> BookingReceipts { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Laptop> Laptops { get; set; }

    public virtual DbSet<LaptopDetail> LaptopDetails { get; set; }

    public virtual DbSet<Manager> Managers { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentRentNotification> StudentRentNotifications { get; set; }

    public virtual DbSet<Technical> Technicals { get; set; }

    public virtual DbSet<TechnicalTicket> TechnicalTickets { get; set; }

    public virtual DbSet<TicketList> TicketLists { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("server=DESKTOP-OOP8VNF;database=swp391_laptop;uid=sa;pwd=123456;TrustServerCertificate=true");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BOOKING__3214EC270F0A34E6");

            entity.ToTable("BOOKING");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.CustomerId).HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("END_TIME");
            entity.Property(e => e.IdNoUrl)
                .HasMaxLength(255)
                .HasColumnName("ID_NO_URL");
            entity.Property(e => e.LaptopId).HasColumnName("LAPTOP_ID");
            entity.Property(e => e.RejectReason).HasColumnName("REJECT_REASON");
            entity.Property(e => e.StaffId).HasColumnName("STAFF_ID");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("START_TIME");
            entity.Property(e => e.StatusId).HasColumnName("STATUS_ID");
            entity.Property(e => e.StudentUrl)
                .HasMaxLength(255)
                .HasColumnName("STUDENT_URL");
            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_PRICE");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("UPDATED_DATE");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_CUSTOMER");

            entity.HasOne(d => d.Laptop).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.LaptopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_LAPTOP");

            entity.HasOne(d => d.Staff).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_BOOKING_STAFF");

            entity.HasOne(d => d.Status).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOOKING_STATUS");
        });

        modelBuilder.Entity<BookingReceipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BOOKING___3214EC272D1BEFDB");

            entity.ToTable("BOOKING_RECEIPT");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BookingId).HasColumnName("BOOKING_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.CustomerId).HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("END_TIME");
            entity.Property(e => e.LateFee)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("LATE_FEE");
            entity.Property(e => e.LateMinutes)
                .HasDefaultValue(0)
                .HasColumnName("LATE_MINUTES");
            entity.Property(e => e.StaffId).HasColumnName("STAFF_ID");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("START_TIME");
            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TOTAL_PRICE");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingReceipts)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RECEIPT_BOOKING");

            entity.HasOne(d => d.Customer).WithMany(p => p.BookingReceipts)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RECEIPT_CUSTOMER");

            entity.HasOne(d => d.Staff).WithMany(p => p.BookingReceipts)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RECEIPT_STAFF");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BRAND__3214EC27E134FCE1");

            entity.ToTable("BRAND");

            entity.HasIndex(e => e.BrandName, "UQ__BRAND__FBF48136D8CE7EF2").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BrandName)
                .HasMaxLength(255)
                .HasColumnName("BRAND_NAME");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CUSTOMER__3214EC27AEAB6965");

            entity.ToTable("CUSTOMER");

            entity.HasIndex(e => e.Email, "UQ__CUSTOMER__161CF72486CADB35").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BlackList).HasColumnName("BLACK_LIST");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.CustomerId).HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("DOB");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("EMAIL");
            entity.Property(e => e.FirstName)
                .HasMaxLength(255)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.IdNo)
                .HasMaxLength(20)
                .HasColumnName("ID_NO");
            entity.Property(e => e.LastName)
                .HasMaxLength(255)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("PHONE");

            entity.HasOne(d => d.CustomerNavigation).WithMany(p => p.Customers)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_CUSTOMER_USER");
        });

        modelBuilder.Entity<Laptop>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LAPTOP__3214EC27A5A3956E");

            entity.ToTable("LAPTOP");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BrandId).HasColumnName("BRAND_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("END_TIME");
            entity.Property(e => e.ImageUrl).HasColumnName("IMAGE_URL");
            entity.Property(e => e.ManagerId).HasColumnName("MANAGER_ID");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("NAME");
            entity.Property(e => e.NewPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("NEW_PRICE");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("PRICE");
            entity.Property(e => e.StatusId).HasColumnName("STATUS_ID");
            entity.Property(e => e.StudentId).HasColumnName("STUDENT_ID");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("UPDATED_DATE");

            entity.HasOne(d => d.Brand).WithMany(p => p.Laptops)
                .HasForeignKey(d => d.BrandId)
                .HasConstraintName("FK_LAPTOP_BRAND");

            entity.HasOne(d => d.Manager).WithMany(p => p.Laptops)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK_LAPTOP_MANAGER");

            entity.HasOne(d => d.Status).WithMany(p => p.Laptops)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_LAPTOP_STATUS");

            entity.HasOne(d => d.Student).WithMany(p => p.Laptops)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_LAPTOP_STUDENT");
        });

        modelBuilder.Entity<LaptopDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LAPTOP_D__3214EC2716554499");

            entity.ToTable("LAPTOP_DETAIL");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Cpu)
                .HasMaxLength(255)
                .HasColumnName("CPU");
            entity.Property(e => e.Gpu)
                .HasMaxLength(255)
                .HasColumnName("GPU");
            entity.Property(e => e.LaptopId).HasColumnName("LAPTOP_ID");
            entity.Property(e => e.Os)
                .HasMaxLength(100)
                .HasColumnName("OS");
            entity.Property(e => e.RamSize)
                .HasMaxLength(50)
                .HasColumnName("RAM_SIZE");
            entity.Property(e => e.RamType)
                .HasMaxLength(50)
                .HasColumnName("RAM_TYPE");
            entity.Property(e => e.ScreenSize)
                .HasMaxLength(50)
                .HasColumnName("SCREEN_SIZE");
            entity.Property(e => e.Storage)
                .HasMaxLength(100)
                .HasColumnName("STORAGE");

            entity.HasOne(d => d.Laptop).WithMany(p => p.LaptopDetails)
                .HasForeignKey(d => d.LaptopId)
                .HasConstraintName("FK_LAPTOP_DETAIL_LAPTOP");
        });

        modelBuilder.Entity<Manager>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MANAGER__3214EC272F38B9AD");

            entity.ToTable("MANAGER");

            entity.HasIndex(e => e.Email, "UQ__MANAGER__161CF724BBDA14C1").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("DOB");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("EMAIL");
            entity.Property(e => e.FirstName)
                .HasMaxLength(255)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.IdNo)
                .HasMaxLength(20)
                .HasColumnName("ID_NO");
            entity.Property(e => e.LastName)
                .HasMaxLength(255)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.ManagerId).HasColumnName("MANAGER_ID");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("PHONE");

            entity.HasOne(d => d.ManagerNavigation).WithMany(p => p.Managers)
                .HasForeignKey(d => d.ManagerId)
                .HasConstraintName("FK_MANAGER_USER");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ROLE__3214EC27FE245F02");

            entity.ToTable("ROLE");

            entity.HasIndex(e => e.RoleName, "UQ__ROLE__2B9B877E4E9CE326").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.RoleName)
                .HasMaxLength(255)
                .HasColumnName("ROLE_NAME");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__STAFF__3214EC27CDB0D0F0");

            entity.ToTable("STAFF");

            entity.HasIndex(e => e.Email, "UQ__STAFF__161CF7245633E229").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("DOB");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("EMAIL");
            entity.Property(e => e.FirstName)
                .HasMaxLength(255)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.IdNo)
                .HasMaxLength(20)
                .HasColumnName("ID_NO");
            entity.Property(e => e.LastName)
                .HasMaxLength(255)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("PHONE");
            entity.Property(e => e.StaffId).HasColumnName("STAFF_ID");

            entity.HasOne(d => d.StaffNavigation).WithMany(p => p.Staff)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_STAFF_USER");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__STATUS__3214EC271C145CCC");

            entity.ToTable("STATUS");

            entity.HasIndex(e => e.StatusName, "UQ__STATUS__064B2D2D899CF141").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.StatusName)
                .HasMaxLength(100)
                .HasColumnName("STATUS_NAME");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__STUDENT__3214EC27197E21E7");

            entity.ToTable("STUDENT");

            entity.HasIndex(e => e.Email, "UQ__STUDENT__161CF724D13B84AE").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("DOB");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("EMAIL");
            entity.Property(e => e.FirstName)
                .HasMaxLength(255)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.IdNo)
                .HasMaxLength(20)
                .HasColumnName("ID_NO");
            entity.Property(e => e.LastName)
                .HasMaxLength(255)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("PHONE");
            entity.Property(e => e.StudentId).HasColumnName("STUDENT_ID");

            entity.HasOne(d => d.StudentNavigation).WithMany(p => p.Students)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_STUDENT_USER");
        });

        modelBuilder.Entity<StudentRentNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__STUDENT___3214EC279158E8BE");

            entity.ToTable("STUDENT_RENT_NOTIFICATION");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BookingId).HasColumnName("BOOKING_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.ManagerId).HasColumnName("MANAGER_ID");
            entity.Property(e => e.Message).HasColumnName("MESSAGE");
            entity.Property(e => e.StudentId).HasColumnName("STUDENT_ID");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("TITLE");

            entity.HasOne(d => d.Booking).WithMany(p => p.StudentRentNotifications)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_STUDENT_NOTIFICATION_BOOKING");

            entity.HasOne(d => d.Manager).WithMany(p => p.StudentRentNotifications)
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_STUDENT_NOTIFICATION_MANAGER");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentRentNotifications)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_STUDENT_NOTIFICATION_STUDENT");
        });

        modelBuilder.Entity<Technical>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TECHNICA__3214EC2713B482B3");

            entity.ToTable("TECHNICAL");

            entity.HasIndex(e => e.Email, "UQ__TECHNICA__161CF72435BD0B6D").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.Dob)
                .HasColumnType("datetime")
                .HasColumnName("DOB");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("EMAIL");
            entity.Property(e => e.FirstName)
                .HasMaxLength(255)
                .HasColumnName("FIRST_NAME");
            entity.Property(e => e.IdNo)
                .HasMaxLength(20)
                .HasColumnName("ID_NO");
            entity.Property(e => e.IsWorking).HasColumnName("IS_WORKING");
            entity.Property(e => e.LastName)
                .HasMaxLength(255)
                .HasColumnName("LAST_NAME");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("PHONE");
            entity.Property(e => e.TechnicalId).HasColumnName("TECHNICAL_ID");

            entity.HasOne(d => d.IsWorkingNavigation).WithMany(p => p.Technicals)
                .HasForeignKey(d => d.IsWorking)
                .HasConstraintName("FK_TECHNICAL_STATUS");

            entity.HasOne(d => d.TechnicalNavigation).WithMany(p => p.Technicals)
                .HasForeignKey(d => d.TechnicalId)
                .HasConstraintName("FK_TECHINICAL_USER");
        });

        modelBuilder.Entity<TechnicalTicket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TECHNICA__3214EC274A59E0EB");

            entity.ToTable("TECHNICAL_TICKET");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BookingId).HasColumnName("BOOKING_ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
            entity.Property(e => e.LaptopId).HasColumnName("LAPTOP_ID");
            entity.Property(e => e.StaffId).HasColumnName("STAFF_ID");
            entity.Property(e => e.StatusId).HasColumnName("STATUS_ID");
            entity.Property(e => e.TechnicalId).HasColumnName("TECHNICAL_ID");
            entity.Property(e => e.TechnicalResponse).HasColumnName("TECHNICAL_RESPONSE");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("UPDATED_DATE");

            entity.HasOne(d => d.Booking).WithMany(p => p.TechnicalTickets)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_TT_BOOKING");

            entity.HasOne(d => d.Laptop).WithMany(p => p.TechnicalTickets)
                .HasForeignKey(d => d.LaptopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TT_LAPTOP");

            entity.HasOne(d => d.Staff).WithMany(p => p.TechnicalTickets)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TT_STAFF");

            entity.HasOne(d => d.Status).WithMany(p => p.TechnicalTickets)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TT_STATUS");

            entity.HasOne(d => d.Technical).WithMany(p => p.TechnicalTickets)
                .HasForeignKey(d => d.TechnicalId)
                .HasConstraintName("FK_TT_TECH");
        });

        modelBuilder.Entity<TicketList>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TICKET_L__3214EC274E835E71");

            entity.ToTable("TICKET_LIST");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.CustomerId).HasColumnName("CUSTOMER_ID");
            entity.Property(e => e.CustomerResponse)
                .HasMaxLength(50)
                .HasColumnName("CUSTOMER_RESPONSE");
            entity.Property(e => e.Description).HasColumnName("DESCRIPTION");
            entity.Property(e => e.ErrorImageUrl).HasColumnName("ERROR_IMAGE_URL");
            entity.Property(e => e.FixedCost)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("FIXED_COST");
            entity.Property(e => e.LaptopId).HasColumnName("LAPTOP_ID");
            entity.Property(e => e.StaffId).HasColumnName("STAFF_ID");
            entity.Property(e => e.StatusId).HasColumnName("STATUS_ID");
            entity.Property(e => e.TechnicalTicketId).HasColumnName("TECHNICAL_TICKET_ID");
            entity.Property(e => e.UpdatedDate)
                .HasColumnType("datetime")
                .HasColumnName("UPDATED_DATE");

            entity.HasOne(d => d.Customer).WithMany(p => p.TicketLists)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TL_CUSTOMER");

            entity.HasOne(d => d.Laptop).WithMany(p => p.TicketLists)
                .HasForeignKey(d => d.LaptopId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TL_LAPTOP");

            entity.HasOne(d => d.Staff).WithMany(p => p.TicketLists)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TL_STAFF");

            entity.HasOne(d => d.Status).WithMany(p => p.TicketLists)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TL_STATUS");

            entity.HasOne(d => d.TechnicalTicket).WithMany(p => p.TicketLists)
                .HasForeignKey(d => d.TechnicalTicketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TL_TECH_TKT");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__USER__3214EC27805F4209");

            entity.ToTable("USER");

            entity.HasIndex(e => e.Email, "UQ__USER__161CF7244AC0581A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("AVATAR_URL");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("CREATED_DATE");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("EMAIL");
            entity.Property(e => e.OtpCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("OTP_CODE");
            entity.Property(e => e.OtpExpiry)
                .HasColumnType("datetime")
                .HasColumnName("OTP_EXPIRY");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("PASSWORD_HASH");
            entity.Property(e => e.RoleId).HasColumnName("ROLE_ID");
            entity.Property(e => e.StatusId).HasColumnName("STATUS_ID");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_USER_ROLE");

            entity.HasOne(d => d.Status).WithMany(p => p.Users)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("FK_USER_STATUS");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
