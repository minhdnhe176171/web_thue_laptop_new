using Microsoft.EntityFrameworkCore;

using web_chothue_laptop.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Đăng ký DbContext
builder.Services.AddDbContext<Swp391LaptopContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Default route → Manager/LaptopRequests
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Manager}/{action=LaptopRequests}/{id?}");

app.Run();
