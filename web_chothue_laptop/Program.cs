using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using web_chothue_laptop.Hubs;
using web_chothue_laptop.Models;
using web_chothue_laptop.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// 1. ADD SERVICES TO THE CONTAINER
// ==============================================

builder.Services.AddControllersWithViews();

// [QUAN TRỌNG] Thêm dịch vụ này để VNPAY Library lấy được IP của khách hàng
builder.Services.AddHttpContextAccessor();

// Cấu hình Authentication (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Chưa đăng nhập thì về đây
        options.AccessDeniedPath = "/Account/AccessDenied"; // Không đủ quyền thì về đây
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    });

// Cấu hình Session (Bắt buộc cho quy trình thanh toán nếu có lưu temp data)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Kết nối Database SQL Server
builder.Services.AddDbContext<Swp391LaptopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// SignalR (Real-time)
builder.Services.AddSignalR();

// Kết nối Redis (Cache & Chat History)
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
        // Trả về kết nối giả lập để app không bị crash nếu Redis chết
        return ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false");
    }
});

// Đăng ký các Service tự viết (DI)
builder.Services.AddScoped<CloudinaryService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton<RedisService>();
// Nếu bạn muốn dùng VnPayLibrary qua DI thì thêm dòng dưới (không bắt buộc nếu dùng 'new VnPayLibrary()')
// builder.Services.AddScoped<VnPayLibrary>(); 

var app = builder.Build();

// ==============================================
// 2. CONFIGURE THE HTTP REQUEST PIPELINE
// ==============================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

// [QUAN TRỌNG] Session phải đặt trước Authentication/Authorization và sau Routing
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Map SignalR Hubs
app.MapHub<ChatHub>("/chathub");
app.MapHub<BookingHub>("/bookinghub");

// Map Controller Routes
// Route cụ thể đặt trước, route mặc định đặt cuối cùng
app.MapControllerRoute(
    name: "createAccount",
    pattern: "CreateAccount/{action=Index}/{id?}",
    defaults: new { controller = "CreateAccount" });

app.MapControllerRoute(
    name: "manageAccount",
    pattern: "ManageAccount/{action=Index}/{id?}",
    defaults: new { controller = "ManageAccount" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();