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
        public IActionResult Index([FromServices] IConfiguration cauHinh)
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
            var homNay = GioVN.HomNay;
            ViewBag.KhuyenMais = _context.KhuyenMais
                .Where(x =>
                    x.TrangThai == TrangThaiKhuyenMai.HoatDong &&
                    x.NgayBatDau <= homNay &&
                    x.NgayKetThuc >= homNay &&
                    (!x.SoLuotDungToiDa.HasValue || x.DatTours.Count(d => d.TrangThai != "DaHuy") < x.SoLuotDungToiDa.Value))
                .OrderBy(x => x.ThuTu)
                .ThenByDescending(x => x.Id)
                .Take(8)
                .ToList();
            //Sắp xếp khuyến mãi theo thứ tự hiển thị, nếu trùng thì ưu tiên mã mới hơn.

            ViewBag.Videos = _context.Videos.AsNoTracking()
                .Where(x => x.DangHien)
                .OrderBy(x => x.ThuTu).ThenBy(x => x.Id)
                .Take(3)
                .ToList();

            ViewBag.BaiViets = _context.BaiViets.AsNoTracking()
                .Where(x => x.TrangThai == BaiViet.DaDang)
                .OrderByDescending(x => x.NgayDang)
                .Take(3)
                .ToList();

            // Điểm trên quả địa cầu: mỗi điểm đến có tour đang bán một chấm, cùng nguồn với Bản đồ tour.
            // Một tour thì bấm sang thẳng trang tour, nhiều tour thì sang danh sách đã lọc theo điểm đến.
            ViewBag.DiemQuaDiaCau = TourTrenBanDo.LayTatCa(_context).CoViTri
                .GroupBy(t => t.DiemDen.ToLower())
                .Select(g => new
                {
                    ten = g.First().DiemDen,
                    lat = Math.Round(g.First().Vi, 3),
                    lon = Math.Round(g.First().Kinh, 3),
                    link = g.Count() == 1
                        ? Url.Action("ChiTiet", "DiaDiem", new { id = g.First().Id })
                        : Url.Action("DanhSach", "DiaDiem", new { keyword = g.First().DiemDen })
                })
                .ToList();

            // Cùng khóa cấu hình với DatTourController, DonNhomController để câu chữ khớp luật đang chạy
            ViewBag.SoGioGiuCho = cauHinh.GetValue("DonNhom:SoGioGiuCho", 24);

            return View(tours);
        }

        // Trang Giới thiệu: thông tin công ty và cách đặt tour nhóm. Mọi con số lấy từ dữ liệu thật,
        // không viết cứng kiểu "100+ tour, 500+ khách" (lỗi 11 trong CLAUDE.md).
        // Trang báo lỗi dùng chung (Program.cs): ma = 404 là không tìm thấy trang, còn lại là lỗi hệ thống.
        // Mở thẳng địa chỉ này (không qua lỗi thật) thì trả 404 để công cụ tìm kiếm không coi đây là trang thường.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Loi(int? ma)
        {
            var loiGoc = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            int maLoi = loiGoc != null ? 500 : ma is >= 400 and <= 599 ? ma.Value : 404;
            Response.StatusCode = maLoi;
            ViewBag.MaLoi = maLoi;
            return View("~/Views/Shared/Error.cshtml");
        }

        public IActionResult GioiThieu([FromServices] IConfiguration cauHinh)
        {
            var homNay = GioVN.HomNay;
            // Cùng khóa cấu hình với DatTourController, DonNhomController để câu chữ khớp luật đang chạy
            ViewBag.SoGioGiuCho = cauHinh.GetValue("DonNhom:SoGioGiuCho", 24);
            // Ảnh điểm đến cho dải ảnh: lấy từ tour đang bán, mỗi điểm đến một ảnh
            ViewBag.AnhDiemDen = _context.Tours
                .Where(t => !t.NgungBan && t.HinhAnh != null && t.HinhAnh != "")
                .OrderBy(t => t.Id)
                .Select(t => new { t.Id, t.TenTour, DiaDiem = t.DiaDiemInfo != null ? t.DiaDiemInfo.TenDiaDiem : null, t.HinhAnh })
                .AsEnumerable()
                .GroupBy(t => (t.DiaDiem ?? t.TenTour).Trim().ToLower())
                .Select(g => g.First())
                .Take(5) // vừa khít lưới: một ảnh lớn 2x2 và bốn ảnh nhỏ
                .Select(t => (t.Id, DiemDen: ToaDoDiaDanh.TenHienThi(t.DiaDiem ?? t.TenTour), Anh: t.HinhAnh!))
                .ToList();
            ViewBag.SoTour = _context.Tours.Count(t => !t.NgungBan);
            ViewBag.SoDiemDen = _context.Tours.Where(t => !t.NgungBan && t.DiaDiemId != null).Select(t => t.DiaDiemId).Distinct().Count();
            ViewBag.SoLichSapToi = _context.LichKhoiHanhTours.Count(l => l.TrangThai != TrangThaiLich.DaHuy && l.NgayKhoiHanh > homNay);
            return View();
        }
    }
}