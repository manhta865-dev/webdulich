using Microsoft.AspNetCore.Mvc;
using webdulich.Models;
using webdulich.Services;

namespace webdulich.ViewComponents
{
    // Khung báo ở góc trang cho khách đang đăng nhập có ghế hoặc đơn nhóm sắp hết hạn giữ chỗ mà chưa trả (RB-47).
    // Cùng mốc giờ với thư nhắc (DonNhom:SoGioNhacTruocHan). Chỉ đọc; không có gì cần nhắc thì không hiện gì.
    public class NhacGiuChoViewComponent : ViewComponent
    {
        private readonly DuLichDbContext _context;
        private readonly IConfiguration _cauHinh;

        public NhacGiuChoViewComponent(DuLichDbContext context, IConfiguration cauHinh)
        {
            _context = context;
            _cauHinh = cauHinh;
        }

        public IViewComponentResult Invoke()
        {
            var taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");
            if (!taiKhoanId.HasValue || HttpContext.Session.GetString("role") is not (null or "user"))
            {
                return Content(string.Empty);
            }

            var ds = NhacHanGiuCho.LayNhacTrenWeb(_context, taiKhoanId.Value, _cauHinh.GetValue("DonNhom:SoGioNhacTruocHan", 6));
            return ds.Count == 0 ? Content(string.Empty) : View(ds);
        }
    }
}
