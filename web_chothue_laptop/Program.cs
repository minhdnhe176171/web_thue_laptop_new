using Microsoft.EntityFrameworkCore;
using web_chothue_laptop.Models;
using web_chothue_laptop.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<Swp391LaptopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// Register Services
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseSession();

app.UseAuthorization();

// Default route → Manager/LaptopRequests
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Manager}/{action=LaptopRequests}/{id?}");

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
