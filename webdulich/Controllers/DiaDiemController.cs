using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class DiaDiemController : Controller
    {
        private readonly DuLichDbContext _context;

        public DiaDiemController(DuLichDbContext context)
        {
            _context = context;
        }
        // Phương thức này trả về một truy vấn IQueryable<Tour> đã bao gồm tất cả các quan hệ cần thiết để hiển thị thông tin chi tiết của tour, bao gồm địa điểm, danh mục, phương tiện, lịch trình, hình ảnh, loại địa hình và phong cách.
        private IQueryable<Tour> LayQueryTourDayDu()
        {
            return _context.Tours
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.DanhMuc)
                .Include(x => x.PhuongTienInfo)
                .Include(x => x.LichTrinhTours)
                    .ThenInclude(x => x.DsDiemThamQuan)
                    .ThenInclude(x => x.DiemThamQuan)
                .Include(x => x.TourHinhAnhs)
                .Include(x => x.TourLoaiDiaHinhs)
                    .ThenInclude(x => x.LoaiDiaHinh)
                .Include(x => x.TourPhongCachs)
                    .ThenInclude(x => x.PhongCach);
        }
        // Truy vấn gọn cho trang danh sách: chỉ nạp những gì thẻ tour hiển thị (địa điểm, danh mục, phương tiện, loại địa hình, phong cách).
        // Bỏ lịch trình và hình ảnh phụ vì danh sách không dùng; tách thành nhiều câu SQL nhỏ và không theo dõi thay đổi để SQL Server không phải xin nhiều bộ nhớ.
        private IQueryable<Tour> LayQueryTourChoDanhSach()
        {
            // Danh sách chỉ hiện tour đang bán
            return _context.Tours
                .AsNoTracking()
                .Where(x => !x.NgungBan)
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.DanhMuc)
                .Include(x => x.PhuongTienInfo)
                .Include(x => x.TourLoaiDiaHinhs)
                    .ThenInclude(x => x.LoaiDiaHinh)
                .Include(x => x.TourPhongCachs)
                    .ThenInclude(x => x.PhongCach)
                .AsSplitQuery();
        }
        // Phương thức này xử lý yêu cầu hiển thị danh sách tour. Nó nhận vào hai tham số: keyword để tìm kiếm theo tên tour, tên địa điểm, tên phương tiện, tên danh mục, tên loại địa hình hoặc tên phong cách; và loaiDiaHinh để lọc theo loại địa hình cụ thể. Kết quả được trả về dưới dạng một danh sách các tour đã được lọc và sắp xếp.
        public IActionResult DanhSach(string keyword, string loaiDiaHinh)
        {
            var ds = LayQueryTourChoDanhSach();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                ds = ds.Where(x =>
                    x.TenTour.Contains(keyword) ||
                    (x.DiaDiemInfo != null && x.DiaDiemInfo.TenDiaDiem.Contains(keyword)) ||
                    (x.PhuongTienInfo != null && x.PhuongTienInfo.TenPhuongTien.Contains(keyword)) ||
                    (x.DanhMuc != null && x.DanhMuc.TenDanhMuc.Contains(keyword)) ||
                    x.TourLoaiDiaHinhs.Any(ldh =>
                        ldh.LoaiDiaHinh != null &&
                        ldh.LoaiDiaHinh.TenLoaiDiaHinh.Contains(keyword)) ||
                    x.TourPhongCachs.Any(pc =>
                        pc.PhongCach != null &&
                        pc.PhongCach.TenPhongCach.Contains(keyword))
                );
            }
            // Lọc theo loại địa hình nếu có
            if (!string.IsNullOrWhiteSpace(loaiDiaHinh))
            {
                loaiDiaHinh = loaiDiaHinh.Trim();

                ds = ds.Where(x =>
                    x.TourLoaiDiaHinhs.Any(ldh =>
                        ldh.LoaiDiaHinh != null &&
                        ldh.LoaiDiaHinh.TenLoaiDiaHinh == loaiDiaHinh)
                );
            }
            // Sắp xếp theo ngày tạo mới nhất
            ViewBag.Keyword = keyword;
            ViewBag.LoaiDiaHinh = loaiDiaHinh;

            return View(ds.ToList());
        }
        // Bản đồ tour: mỗi tour đang bán là một chấm trên bản đồ Việt Nam, kèm giá thấp nhất và ngày đi gần nhất.
        // Tọa độ lấy điểm cuối lịch trình admin đã nhập; chưa có thì tra theo tên địa điểm (ToaDoDiaDanh).
        public IActionResult BanDo()
        {
            var (ds, chuaCoViTri) = TourTrenBanDo.LayTatCa(_context);
            ViewBag.ChuaCoViTri = chuaCoViTri;
            return View(ds);
        }

        // Lịch khởi hành theo tháng: mỗi ngày hiện các tour đi hôm đó, màu theo số chỗ còn. Lọc theo điểm đến.
        public IActionResult LichKhoiHanh(int? thang, int? nam, int? diaDiem)
        {
            var homNay = GioVN.HomNay;
            int t = thang is >= 1 and <= 12 ? thang.Value : homNay.Month;
            int n = nam is >= 2000 and <= 2100 ? nam.Value : homNay.Year;
            var dau = new DateTime(n, t, 1);
            var cuoi = dau.AddMonths(1);

            var q = _context.LichKhoiHanhTours.AsNoTracking()
                .Where(l => l.TrangThai != TrangThaiLich.DaHuy && l.NgayKhoiHanh >= dau && l.NgayKhoiHanh < cuoi && !l.Tour!.NgungBan);
            if (diaDiem.HasValue)
            {
                q = q.Where(l => l.Tour!.DiaDiemId == diaDiem.Value);
            }

            var m = new LichThangViewModel
            {
                Thang = t,
                Nam = n,
                DiaDiemId = diaDiem,
                // Tự có điểm đến mới khi admin thêm tour đang bán ở địa điểm đó; tên đưa về cùng kiểu viết, xếp theo tiếng Việt
                DsDiaDiem = _context.DiaDiems.AsNoTracking()
                    .Where(d => _context.Tours.Any(x => x.DiaDiemId == d.MaDiaDiem && !x.NgungBan))
                    .Select(d => new { d.MaDiaDiem, d.TenDiaDiem }).ToList()
                    .Select(d => (d.MaDiaDiem, ToaDoDiaDanh.TenHienThi(d.TenDiaDiem)))
                    .OrderBy(d => d.Item2, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), true))
                    .ToList(),
                DsLich = q.Include(l => l.Tour)
                    .OrderBy(l => l.NgayKhoiHanh).ThenBy(l => l.GioKhoiHanh)
                    .ToList()
                    .Select(l => new LichTrongNgay
                    {
                        LichId = l.Id,
                        TourId = l.TourId,
                        TenTour = l.Tour?.TenTour ?? string.Empty,
                        Ngay = l.NgayKhoiHanh,
                        Gio = l.GioKhoiHanh,
                        SoChoCon = l.SoChoCon,
                        Gia = l.GiaNguoiLonHienThi,
                        DatDuoc = l.ChoPhepDatTour && l.SoChoCon > 0
                    }).ToList()
            };
            return View(m);
        }

        // Tour kèm lịch trình đã xếp theo ngày, chỉ khi tour có điểm xuất phát và mọi ngày đều có tọa độ (đủ để vẽ bản đồ, chỉ đường)
        private (Tour Tour, List<LichTrinhTour> DsNgay)? LayTourDuToaDo(int id, bool kemDiemThamQuan = false)
        {
            var q = _context.Tours.AsNoTracking().Include(x => x.LichTrinhTours).AsQueryable();
            if (kemDiemThamQuan)
            {
                q = q.Include(x => x.LichTrinhTours).ThenInclude(x => x.DsDiemThamQuan).ThenInclude(x => x.DiemThamQuan);
            }
            var tour = q.FirstOrDefault(x => x.Id == id);
            if (tour == null || !tour.ViDoXuatPhat.HasValue || !tour.KinhDoXuatPhat.HasValue)
            {
                return null;
            }
            var dsNgay = tour.LichTrinhTours.OrderBy(x => x.NgayThu).ToList();
            if (dsNgay.Count == 0 || dsNgay.Any(x => !x.ViDo.HasValue || !x.KinhDo.HasValue))
            {
                return null;
            }
            return (tour, dsNgay);
        }

        // Khám phá 3D: trang toàn màn hình gồm bản đồ 3D (vệ tinh, địa hình), chỉ đường từng bước, Google Maps. Chỉ đọc.
        public IActionResult KhamPha(int id, [FromServices] IConfiguration cauHinh)
        {
            var duLieu = LayTourDuToaDo(id, kemDiemThamQuan: true);
            // Khóa Maps Embed API (không bắt buộc): có thì Street View nhúng theo cách chính thức. Không bao giờ ghi khóa vào mã nguồn.
            var khoa = cauHinh["GoogleMaps:KhoaNhung"];
            if (duLieu == null)
            {
                if (!_context.Tours.Any(x => x.Id == id))
                {
                    return NotFound();
                }
                TempData["Loi"] = "Tour này chưa có đủ tọa độ lịch trình nên chưa xem được bản đồ 3D.";
                return RedirectToAction(nameof(ChiTiet), new { id });
            }

            var (tour, dsNgay) = duLieu.Value;
            var xuatPhat = (Vi: tour.ViDoXuatPhat!.Value, Kinh: tour.KinhDoXuatPhat!.Value);
            var dsDiem = dsNgay.Select(x => (Vi: x.ViDo!.Value, Kinh: x.KinhDo!.Value)).ToList();

            var m = new KhamPha3DViewModel
            {
                TourId = tour.Id,
                TenTour = tour.TenTour.Trim(),
                ViXuatPhat = xuatPhat.Vi,
                KinhXuatPhat = xuatPhat.Kinh,
                LinkCaHanhTrinh = Services.LienKetBanDo.ChiDuong(xuatPhat, dsDiem[^1], dsDiem.Take(dsDiem.Count - 1).ToList()),
                DsNgay = dsNgay.Select((x, i) => new DiemKhamPha3D
                {
                    Ngay = x.NgayThu,
                    TieuDe = x.TieuDe,
                    BuaAn = x.BuaAn,
                    Vi = dsDiem[i].Vi,
                    Kinh = dsDiem[i].Kinh,
                    DuongDi = x.DuongDi,
                    LinkChiDuong = Services.LienKetBanDo.ChiDuong(i == 0 ? xuatPhat : dsDiem[i - 1], dsDiem[i]),
                    LinkStreetView = Services.LienKetBanDo.StreetView(dsDiem[i].Vi, dsDiem[i].Kinh),
                    LinkNhung = Services.LienKetBanDo.Nhung(dsDiem[i].Vi, dsDiem[i].Kinh),
                    Link360 = Services.LienKetBanDo.NhungStreetView(dsDiem[i].Vi, dsDiem[i].Kinh, khoa),
                    DsDiem = x.DsDiemThamQuan.OrderBy(d => d.ThuTu).Where(d => d.DiemThamQuan != null).Select(d => new DiemThamQuan3D
                    {
                        Ten = d.DiemThamQuan!.Ten,
                        Vi = d.DiemThamQuan.ViDo,
                        Kinh = d.DiemThamQuan.KinhDo,
                        LinkStreetView = Services.LienKetBanDo.StreetView(d.DiemThamQuan.ViDo, d.DiemThamQuan.KinhDo),
                        LinkNhung = Services.LienKetBanDo.Nhung(d.DiemThamQuan.ViDo, d.DiemThamQuan.KinhDo, 16),
                        // Có mã nhúng đúng tấm ảnh 360 (Mo24) thì dùng, không thì Street View đường phố gần tọa độ nhất
                        Link360 = d.DiemThamQuan.LinkNhung360
                            ?? Services.LienKetBanDo.NhungStreetView(d.DiemThamQuan.ViDo, d.DiemThamQuan.KinhDo, khoa)
                    }).ToList()
                }).ToList()
            };
            return View(m);
        }

        // Chỉ đường từng bước của một ngày (từ điểm trước tới điểm của ngày đó), trang Khám phá 3D gọi khi bấm "Dẫn đường"
        [HttpGet]
        public async Task<IActionResult> ChiDuong(int id, int ngay)
        {
            var duLieu = LayTourDuToaDo(id);
            if (duLieu == null)
            {
                return NotFound();
            }
            var (tour, dsNgay) = duLieu.Value;
            int i = dsNgay.FindIndex(x => x.NgayThu == ngay);
            if (i < 0)
            {
                return NotFound();
            }

            decimal kinhDau = i == 0 ? tour.KinhDoXuatPhat!.Value : dsNgay[i - 1].KinhDo!.Value;
            decimal viDau = i == 0 ? tour.ViDoXuatPhat!.Value : dsNgay[i - 1].ViDo!.Value;
            var kq = await Services.DuongXeChay.LayChiDanAsync(kinhDau, viDau, dsNgay[i].KinhDo!.Value, dsNgay[i].ViDo!.Value);
            if (kq == null)
            {
                return StatusCode(503, new { loi = "Chưa lấy được chỉ đường từ máy chủ bản đồ. Bạn thử lại sau, hoặc mở Google Maps." });
            }

            return Json(new
            {
                km = Math.Round(kq.Km, 1),
                phut = Math.Round(kq.Phut),
                buoc = kq.DsBuoc.Select(b => new { chu = b.Chu, km = Math.Round(b.Km, 1) })
            });
        }

        // Phương thức này xử lý yêu cầu hiển thị trang chủ của địa điểm. Nó đơn giản chuyển hướng đến phương thức DanhSach để hiển thị danh sách tour.
        public IActionResult Index()
        {
            return RedirectToAction("DanhSach");
        }
        // Phương thức này xử lý yêu cầu hiển thị chi tiết của một tour cụ thể. Nó nhận vào tham số id để xác định tour cần hiển thị. Nếu tour không tồn tại, nó trả về lỗi 404 Not Found. Nếu tour tồn tại, nó cũng lấy danh sách lịch khởi hành sắp tới của tour đó và truyền cả thông tin tour và lịch khởi hành vào view để hiển thị.
        public IActionResult ChiTiet(int id)
        {
            var tour = LayQueryTourDayDu()
                .FirstOrDefault(x => x.Id == id);

            if (tour == null)
            {
                return NotFound();
            }
            // Lấy danh sách lịch khởi hành sắp tới của tour, loại bỏ những lịch đã hủy và những lịch đã khởi hành trước ngày hôm nay
            var ngayMai = GioVN.HomNay.AddDays(1);

            // Tour ngừng bán thì không còn lịch để đặt; lịch tạm ngưng hoặc đã hủy cũng không hiện
            ViewBag.LichKhoiHanh = _context.LichKhoiHanhTours
                .Where(x => x.TourId == id
                    && !tour.NgungBan
                    && x.TrangThai == TrangThaiLich.MoBan
                    && x.NgayKhoiHanh >= ngayMai)
                .OrderBy(x => x.NgayKhoiHanh)
                .ToList();

            ViewBag.Videos = _context.Videos.AsNoTracking()
                .Where(x => x.TourId == id && x.DangHien)
                .OrderBy(x => x.ThuTu).ThenBy(x => x.Id)
                .ToList();

            // Đánh giá đang hiện của mọi lịch thuộc tour (RB-43); điểm trung bình tính tại chỗ, không lưu
            var (trungBinh, soLuot) = DanhGiaDoc.TongHop(_context, id);
            ViewBag.DiemTrungBinh = trungBinh;
            ViewBag.SoDanhGia = soLuot;
            ViewBag.DemSao = _context.DanhGiaTours.Where(d => d.DangHien && d.LichKhoiHanhTour!.TourId == id)
                .GroupBy(d => d.SoSao).Select(g => new { g.Key, SoLuot = g.Count() })
                .AsEnumerable().ToDictionary(x => x.Key, x => x.SoLuot);
            ViewBag.DanhGia = _context.DanhGiaTours.AsNoTracking()
                .Where(d => d.DangHien && d.LichKhoiHanhTour!.TourId == id)
                .OrderByDescending(d => d.NgaySua ?? d.NgayTao)
                .Take(20)
                .Select(d => new { d.TaiKhoan!.HoTen, d.SoSao, d.NoiDung, d.LichKhoiHanhTour!.NgayKhoiHanh, NgayViet = d.NgaySua ?? d.NgayTao })
                .AsEnumerable()
                .Select(d => new DanhGiaHienThi
                {
                    TenNguoi = DanhGiaDoc.TenRutGon(d.HoTen),
                    SoSao = d.SoSao,
                    NoiDung = d.NoiDung,
                    NgayDi = d.NgayKhoiHanh,
                    NgayViet = d.NgayViet
                })
                .ToList();

            // Tour tương tự: đang bán, cùng điểm đến hoặc cùng loại địa hình; cùng điểm đến xếp trước. Chỉ đọc.
            var dsDiaHinh = tour.TourLoaiDiaHinhs.Select(x => x.LoaiDiaHinhId).ToList();
            ViewBag.TourTuongTu = LayQueryTourChoDanhSach()
                .Where(x => x.Id != id
                    && ((tour.DiaDiemId != null && x.DiaDiemId == tour.DiaDiemId)
                        || x.TourLoaiDiaHinhs.Any(l => dsDiaHinh.Contains(l.LoaiDiaHinhId))))
                .OrderByDescending(x => tour.DiaDiemId != null && x.DiaDiemId == tour.DiaDiemId)
                .ThenBy(x => x.TenTour)
                .Take(4)
                .ToList();

            return View(tour);
        }
    }
}