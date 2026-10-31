using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddSession();

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