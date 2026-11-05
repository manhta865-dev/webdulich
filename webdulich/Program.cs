using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using Microsoft.AspNetCore.Identity;

// Số hiện kiểu Việt Nam (3.000.000) trên mọi máy chủ. Giữ gốc en-US và dấu thập phân là dấu chấm để đọc đúng
// giá trị ô số từ form (10.5); gõ 1.500.000 thì bị từ chối chứ không bị hiểu sai.
var dinhDang = (System.Globalization.CultureInfo)System.Globalization.CultureInfo.GetCultureInfo("en-US").Clone();
dinhDang.NumberFormat.NumberGroupSeparator = ".";
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = dinhDang;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = dinhDang;

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

// Đăng nhập Google: chỉ bật khi có khóa trong cấu hình (user-secrets trên máy, cấu hình riêng trên host), không có thì nút tự ẩn.
// Cookie tạm chỉ giữ kết quả từ Google tới lúc TaiKhoan/GoogleXong ghi Session rồi bị xóa; đăng nhập thật vẫn chạy bằng Session.
var googleId = builder.Configuration["Google:ClientId"];
var googleSecret = builder.Configuration["Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleId) && !string.IsNullOrEmpty(googleSecret))
{
    builder.Services.AddAuthentication()
        .AddCookie(webdulich.Controllers.TaiKhoanController.CookieTamGoogle, o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            o.Cookie.SecurePolicy = cheDoSecure;
        })
        .AddGoogle(o =>
        {
            o.ClientId = googleId;
            o.ClientSecret = googleSecret;
            o.SignInScheme = webdulich.Controllers.TaiKhoanController.CookieTamGoogle;
            o.ClaimActions.MapJsonKey("email_verified", "email_verified");
            // Người dùng bấm hủy ở Google, hoặc Google báo lỗi: về trang đăng nhập có lời giải thích thay vì trang lỗi
            o.Events.OnRemoteFailure = ctx =>
            {
                ctx.Response.Redirect("/TaiKhoan/DangNhap?loiGoogle=1");
                ctx.HandleResponse();
                return Task.CompletedTask;
            };
        });
}

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