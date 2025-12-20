using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using web_chothue_laptop.Hubs;
using web_chothue_laptop.Models;
using web_chothue_laptop.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Nếu chưa đăng nhập thì đá về đây
        options.AccessDeniedPath = "/Account/AccessDenied"; // Nếu không đủ quyền (Staff vào trang Tech) thì đá về đây
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<Swp391LaptopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Add SignalR
builder.Services.AddSignalR();

// Add Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var configurationOptions = new ConfigurationOptions
        {
            EndPoints = { redisConnectionString },
            AbortOnConnectFail = false,
            ConnectRetry = 3,
            ConnectTimeout = 5000
        };
        return ConnectionMultiplexer.Connect(configurationOptions);
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to connect to Redis. Chat history will not be persisted. Error: {Message}", ex.Message);
        // Return a dummy connection multiplexer that won't crash the app
        // In production, you might want to handle this differently
        return ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
    }
});

// Register Services
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<RedisService>();
builder.Services.AddScoped<RagService>();
builder.Services.AddHttpClient<AIChatService>();
builder.Services.AddScoped<AIChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<ChatHub>("/chathub");
app.MapHub<BookingHub>("/bookinghub");
app.MapHub<AIChatHub>("/aichathub");



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

//app.MapControllerRoute(
//    name: "manager",
//    pattern: "{controller=Manager}/{action=LaptopRequests}/{id?}");

app.Run();