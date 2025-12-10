using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<Swp391LaptopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "createAccount",
    pattern: "CreateAccount/{action=Index}/{id?}",
    defaults: new { controller = "CreateAccount" });

// ManageAccount routes
app.MapControllerRoute(
    name: "manageAccount",
    pattern: "ManageAccount/{action=Index}/{id?}",
    defaults: new { controller = "ManageAccount" });

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
