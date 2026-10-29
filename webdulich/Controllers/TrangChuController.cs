using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class TrangChuController : Controller
    {
        private readonly DuLichDbContext _context;

        public TrangChuController(DuLichDbContext context)
        {
            _context = context;
        }
        // Dùng để nhận DuLichDbContext, tức là kết nối database.(trên)
        // Hàm này tạo truy vấn lấy tour kèm đầy đủ dữ liệu liên quan.
        private IQueryable<Tour> LayQueryTourDayDu()
        {
            // Trang chủ chỉ hiện tour đang bán
            return _context.Tours
                .Where(x => !x.NgungBan)
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.DanhMuc)
                .Include(x => x.PhuongTienInfo)
                .Include(x => x.TourLoaiDiaHinhs)
                    .ThenInclude(x => x.LoaiDiaHinh)
                .Include(x => x.TourPhongCachs)
                    .ThenInclude(x => x.PhongCach);
        }
        //Đây là action mở trang chủ.
        public IActionResult Index()
        {

            //Nếu người đăng nhập là admin thì tự chuyển về trang quản trị.
            if (HttpContext.Session.GetString("role") == "admin")
            {
                return RedirectToAction("TongQuan", "Admin");
            }
            // Lấy 3 tour đầu tiên để hiển thị ở trang chủ.
            var tours = LayQueryTourDayDu()
                .Take(3)
                .ToList();
            //Lấy tối đa 6 tour có danh mục:
            ViewBag.TourTrongNuoc = LayQueryTourDayDu()
      .Where(x => x.DanhMuc != null && x.DanhMuc.TenDanhMuc == "Trong nước")
      .Take(6)
      .ToList();

            ViewBag.TourNgoaiNuoc = LayQueryTourDayDu()
                .Where(x => x.DanhMuc != null && x.DanhMuc.TenDanhMuc == "Ngoài nước")
                .Take(6)
                .ToList();

            ViewBag.TopKhach = _context.DatTours
                .GroupBy(x => x.TenKhach)
                .Select(g => new
                {
                    TenKhach = g.Key,
                    SoLan = g.Count()
                })
                .OrderByDescending(x => x.SoLan)
                .Take(5)
                .ToList();

            // Tour của đơn lấy qua lịch khởi hành
            ViewBag.TopTour = _context.DatTours
                .Where(x => x.LichKhoiHanhTour != null && x.LichKhoiHanhTour.Tour != null)
                .GroupBy(x => x.LichKhoiHanhTour!.Tour!.TenTour)
                .Select(g => new
                {
                    TenTour = g.Key,
                    SoLan = g.Count()
                })
                .OrderByDescending(x => x.SoLan)
                .Take(5)
                .ToList();
            // 10. Lấy khuyến mãi đang hoạt động
            var homNay = DateTime.Today;
            ViewBag.KhuyenMais = _context.KhuyenMais
                .Where(x =>
                    x.TrangThai == TrangThaiKhuyenMai.HoatDong &&
                    x.NgayBatDau <= homNay &&
                    x.NgayKetThuc >= homNay &&
                    (!x.SoLuotDungToiDa.HasValue || x.DatTours.Count(d => d.TrangThai != "DaHuy" && d.TrangThaiThanhToan != "Đã hủy") < x.SoLuotDungToiDa.Value))
                .OrderBy(x => x.ThuTu)
                .ThenByDescending(x => x.Id)
                .Take(8)
                .ToList();
            //Sắp xếp khuyến mãi theo thứ tự hiển thị, nếu trùng thì ưu tiên mã mới hơn.
            return View(tours);
        }
    }
}