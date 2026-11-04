using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Cookie chỉ gửi qua HTTPS khi chạy thật; trên máy (Development) còn dùng được cả http://localhost
var cheDoSecure = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;

// Add services
builder.Services.AddControllersWithViews()
    .AddCookieTempDataProvider(o => o.Cookie.SecurePolicy = cheDoSecure);

// Lax chứ không Strict: mở link mời từ Zalo, Messenger vẫn còn đăng nhập
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(60);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = cheDoSecure;
});
builder.Services.AddAntiforgery(o => o.Cookie.SecurePolicy = cheDoSecure);

// 7 ngày cho đồ án: nếu HTTPS của host trục trặc, trình duyệt không bị kẹt lâu
builder.Services.AddHsts(o => o.MaxAge = TimeSpan.FromDays(7));

builder.Services.AddSingleton<webdulich.Services.GioiHanDangNhap>();

builder.Services.AddDbContext<DuLichDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DuLichConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<DuLichDbContext>()
    .AddDefaultTokenProviders();

// N5: quét đơn nhóm hết hạn giữ chỗ theo chu kỳ (gia hạn, nhả ghế chưa trả, chốt hoặc hủy)
builder.Services.AddHostedService<webdulich.Services.XuLyHetHanDonNhom>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TrangChu}/{action=Index}/{id?}");

app.Run();