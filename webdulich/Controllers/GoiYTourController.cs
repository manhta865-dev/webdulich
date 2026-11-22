using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class GoiYTourController : Controller
    {
        private readonly DuLichDbContext _context;
        // Controller này xử lý các yêu cầu liên quan đến gợi ý tour dựa trên các tiêu chí mà người dùng đã chọn. Nó cung cấp một trang để người dùng có thể chọn các tiêu chí như loại tour, địa điểm, loại địa hình, phong cách, phương tiện di chuyển, ngân sách tối đa và số ngày. Dựa trên các tiêu chí này, controller sẽ tính toán điểm gợi ý cho từng tour và trả về danh sách các tour phù hợp nhất để hiển thị cho người dùng.
        public GoiYTourController(DuLichDbContext context)
        {
            _context = context;
        }
        // Phương thức này xử lý yêu cầu hiển thị trang gợi ý tour. Nếu người dùng đã đăng nhập với vai trò admin, họ sẽ được chuyển hướng đến trang quản trị. Ngược lại, một model GoiYTourPageViewModel sẽ được tạo và dữ liệu lựa chọn sẽ được nạp vào model này. Sau đó, phương thức LayDanhSachGoiY sẽ được gọi để lấy danh sách các tour phù hợp dựa trên tiêu chí đã chọn, và kết quả sẽ được trả về để hiển thị trên trang.
        [HttpGet]
        public IActionResult Index([FromServices] IConfiguration cauHinh)
        {
            if (HttpContext.Session.GetString("role") == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            // Tạo một instance của view model để chứa dữ liệu lựa chọn và kết quả gợi ý
            var model = new GoiYTourPageViewModel { CoAi = new webdulich.Services.GoiYBangGemini(cauHinh).CoKhoa };
            NapDuLieuLuaChon(model);
            model.KetQua = LayDanhSachGoiY(model.TieuChi);

            return View(model);
        }
        // Phương thức này xử lý yêu cầu khi người dùng gửi form với các tiêu chí đã chọn. Tương tự như phương thức GET, nếu người dùng là admin, họ sẽ được chuyển hướng đến trang quản trị. Nếu không, dữ liệu lựa chọn sẽ được nạp lại vào model và phương thức LayDanhSachGoiY sẽ được gọi để tính toán lại danh sách các tour phù hợp dựa trên tiêu chí mới. Kết quả sẽ được trả về để hiển thị trên trang.
        [HttpPost]
        public IActionResult Index(GoiYTourPageViewModel model, [FromServices] IConfiguration cauHinh)
        {
            if (HttpContext.Session.GetString("role") == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            // Nạp lại dữ liệu lựa chọn vào model để đảm bảo các dropdown vẫn có dữ liệu sau khi submit form
            NapDuLieuLuaChon(model);
            model.CoAi = new webdulich.Services.GoiYBangGemini(cauHinh).CoKhoa;
            model.KetQua = LayDanhSachGoiY(model.TieuChi);

            return View(model);
        }

        private const int SoLanAiMoiGio = 8;
        private const int DoDaiMoTaToiDa = 500;

        // Gợi ý bằng AI: khách mô tả chuyến đi bằng lời, Gemini chọn tối đa 3 tour trong các tour đang bán.
        // Giới hạn số lần mỗi phiên trong một giờ để không ai dùng hết hạn mức của khóa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HoiAi(string? moTaAi, [FromServices] IConfiguration cauHinh)
        {
            if (HttpContext.Session.GetString("role") == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            var ai = new webdulich.Services.GoiYBangGemini(cauHinh);
            var model = new GoiYTourPageViewModel { CoAi = ai.CoKhoa, MoTaAi = moTaAi?.Trim() };
            NapDuLieuLuaChon(model);
            model.KetQua = LayDanhSachGoiY(model.TieuChi);

            if (string.IsNullOrWhiteSpace(model.MoTaAi) || model.MoTaAi.Length < 5)
            {
                model.LoiAi = "Bạn mô tả chuyến đi muốn đi thêm một chút, ví dụ: nhóm 6 bạn, 3 ngày, thích núi, dưới 5 triệu mỗi người.";
                return View("Index", model);
            }
            if (model.MoTaAi.Length > DoDaiMoTaToiDa)
            {
                model.MoTaAi = model.MoTaAi[..DoDaiMoTaToiDa];
            }

            // Đếm lượt trong phiên: quá SoLanAiMoiGio lượt trong 60 phút thì nhờ dùng phần chọn tiêu chí
            var bayGio = DateTime.UtcNow;
            var tu = long.TryParse(HttpContext.Session.GetString("aiTu"), out var ticks) ? new DateTime(ticks, DateTimeKind.Utc) : bayGio;
            var soLan = bayGio - tu > TimeSpan.FromHours(1) ? 0 : HttpContext.Session.GetInt32("aiLan") ?? 0;
            if (soLan == 0)
            {
                tu = bayGio;
            }
            if (soLan >= SoLanAiMoiGio)
            {
                model.LoiAi = $"Bạn đã hỏi AI {SoLanAiMoiGio} lần trong một giờ. Bạn thử lại sau, hoặc dùng phần chọn tiêu chí bên dưới.";
                return View("Index", model);
            }
            HttpContext.Session.SetString("aiTu", tu.Ticks.ToString());
            HttpContext.Session.SetInt32("aiLan", soLan + 1);

            // Chỉ gửi thông tin công khai của tour đang bán, kèm số lịch khởi hành sắp tới còn chỗ
            var homNay = GioVN.HomNay;
            var dsTour = LayQueryTourDayDu().AsNoTracking().ToList();
            var soLich = _context.LichKhoiHanhTours.AsNoTracking()
                .Where(l => l.TrangThai == TrangThaiLich.MoBan && l.NgayKhoiHanh > homNay && l.SoChoCon > 0)
                .GroupBy(l => l.TourId).Select(g => new { g.Key, SoLuong = g.Count() })
                .ToDictionary(x => x.Key, x => x.SoLuong);
            var dsChoAi = dsTour.Select(t => new webdulich.Services.TourChoAi
            {
                Id = t.Id,
                Ten = t.TenTour.Trim(),
                DiemDen = t.TenDiaDiemHienThi,
                SoNgay = t.SoNgay,
                GiaTu = t.Gia,
                DiaHinh = t.LoaiDiaHinhHienThi,
                PhongCach = t.PhongCachHienThi,
                PhuongTien = t.PhuongTienHienThi,
                MoTa = (t.MoTa ?? string.Empty).Length > 200 ? t.MoTa![..200] : t.MoTa ?? string.Empty,
                SoLichSapToi = soLich.TryGetValue(t.Id, out var n) ? n : 0
            }).ToList();

            var (ketQua, loi) = await ai.GoiYAsync(model.MoTaAi, dsChoAi);
            if (ketQua == null)
            {
                model.LoiAi = loi;
                return View("Index", model);
            }

            model.TomTatAi = ketQua.TomTat;
            model.KetQuaAi = ketQua.DsTour
                .Select(c => (Chon: c, Tour: dsTour.FirstOrDefault(t => t.Id == c.Id)))
                .Where(x => x.Tour != null)
                .Select(x => new TourGoiYItemViewModel { Tour = x.Tour!, LyDoGoiY = x.Chon.LyDo, TenDiaDiemHienThi = x.Tour!.TenDiaDiemHienThi })
                .ToList();
            return View("Index", model);
        }
        // Phương thức này trả về một truy vấn IQueryable<Tour> đã bao gồm tất cả các quan hệ cần thiết để hiển thị thông tin chi tiết của tour, bao gồm địa điểm, danh mục, phương tiện, loại địa hình và phong cách. Đây là cơ sở để các phương thức khác có thể truy vấn và lọc dữ liệu tour một cách hiệu quả.
        private IQueryable<Tour> LayQueryTourDayDu()
        {
            // Chỉ gợi ý tour đang bán
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
        // Phương thức này nạp dữ liệu lựa chọn cho các dropdown trên trang gợi ý tour. Nó lấy dữ liệu từ cơ sở dữ liệu để tạo danh sách các lựa chọn cho loại tour, địa điểm, loại địa hình, phong cách và phương tiện di chuyển. Ngoài ra, nó cũng tạo danh sách lựa chọn cho số ngày với các giá trị cố định.
        private void NapDuLieuLuaChon(GoiYTourPageViewModel model)
        {
            //
            model.LoaiTourOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Trong nước", Text = "Trong nước" },
                new SelectListItem { Value = "Ngoài nước", Text = "Ngoài nước" }
            };
            // Lấy danh sách địa điểm từ cơ sở dữ liệu và sắp xếp theo tên
            model.DiaDiemOptions = _context.DiaDiems
                .OrderBy(x => x.TenDiaDiem)
                .Select(x => new SelectListItem
                {
                    Value = x.MaDiaDiem.ToString(),
                    Text = x.TenDiaDiem
                })
                .ToList();
            // 
            model.LoaiDiaHinhOptions = _context.LoaiDiaHinhs
                .OrderBy(x => x.TenLoaiDiaHinh)
                .Select(x => new SelectListItem
                {
                    Value = x.TenLoaiDiaHinh,
                    Text = x.TenLoaiDiaHinh
                })
                .ToList();

            model.PhongCachOptions = _context.PhongCachs
                .OrderBy(x => x.TenPhongCach)
                .Select(x => new SelectListItem
                {
                    Value = x.TenPhongCach,
                    Text = x.TenPhongCach
                })
                .ToList();

            model.PhuongTienOptions = _context.PhuongTiens
                .OrderBy(x => x.TenPhuongTien)
                .Select(x => new SelectListItem
                {
                    Value = x.TenPhuongTien,
                    Text = x.TenPhuongTien
                })
                .ToList();
            // Tạo danh sách lựa chọn cho số ngày với các giá trị cố định
            model.SoNgayOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "1 ngày" },
                new SelectListItem { Value = "2", Text = "2 ngày" },
                new SelectListItem { Value = "3", Text = "3 ngày" },
                new SelectListItem { Value = "4", Text = "4 ngày" },
                new SelectListItem { Value = "5", Text = "5 ngày trở lên" }
            };
        }
        // Phương thức này là lõi của controller, nó thực hiện việc tính toán điểm gợi ý cho từng tour dựa trên các tiêu chí mà người dùng đã chọn. Điểm gợi ý được tính dựa trên việc so sánh thông tin của tour với các tiêu chí, cũng như số lượt đặt tour để ưu tiên những tour được nhiều người quan tâm. Kết quả cuối cùng là một danh sách các tour được sắp xếp theo điểm gợi ý và số lượt đặt, chỉ lấy top 9 tour phù hợp nhất để hiển thị.
        private List<TourGoiYItemViewModel> LayDanhSachGoiY(TieuChiGoiYViewModel tieuChi)
        {
            var tours = LayQueryTourDayDu().ToList();
            // Tạo một dictionary để đếm số lượt đặt cho mỗi tour, giúp việc truy cập nhanh khi tính điểm gợi ý
            // Tour của đơn lấy qua lịch khởi hành
            var bookingDict = _context.DatTours
                .Where(x => x.LichKhoiHanhTour != null)
                .GroupBy(x => x.LichKhoiHanhTour!.TourId)
                .ToDictionary(g => g.Key, g => g.Count());
            // Danh sách kết quả gợi ý sẽ chứa các tour cùng với điểm gợi ý, số lượt đặt và lý do gợi ý
            var ketQua = new List<TourGoiYItemViewModel>();
            // Duyệt qua từng tour để tính điểm gợi ý dựa trên các tiêu chí đã chọn
            foreach (var tour in tours)
            {
                int diem = 0;
                var lyDo = new List<string>();
                int soLuotDat = bookingDict.ContainsKey(tour.Id) ? bookingDict[tour.Id] : 0;

                var dsLoaiTour = LayLoaiTour(tour);
                var tenPhuongTien = tour.PhuongTienInfo?.TenPhuongTien ?? "";
                var dsDiaHinh = LayLoaiDiaHinh(tour);
                var dsPhongCach = LayPhongCach(tour);
                // Kiểm tra từng tiêu chí và cộng điểm nếu tour phù hợp với tiêu chí đó
                if (!string.IsNullOrWhiteSpace(tieuChi.LoaiTour))
                {
                    if (!dsLoaiTour.Any(x => KHopLuaChon(x, tieuChi.LoaiTour)))
                    {
                        continue;
                    }

                    diem += 5;
                    lyDo.Add("Đúng loại tour");
                }
                // Kiểm tra tiêu chí địa điểm
                if (tieuChi.DiaDiemId.HasValue)
                {
                    if (!tour.DiaDiemId.HasValue || tour.DiaDiemId.Value != tieuChi.DiaDiemId.Value)
                    {
                        continue;
                    }

                    diem += 5;
                    lyDo.Add("Đúng địa điểm");
                }

                if (!string.IsNullOrWhiteSpace(tieuChi.LoaiDiaHinh))
                {
                    if (!dsDiaHinh.Any(x => BangNhau(x, tieuChi.LoaiDiaHinh)))
                    {
                        continue;
                    }

                    diem += 5;
                    lyDo.Add("Đúng địa hình");
                }

                if (!string.IsNullOrWhiteSpace(tieuChi.PhongCach))
                {
                    if (!dsPhongCach.Any(x => BangNhau(x, tieuChi.PhongCach)))
                    {
                        continue;
                    }

                    diem += 5;
                    lyDo.Add("Đúng phong cách");
                }

                if (!string.IsNullOrWhiteSpace(tieuChi.PhuongTien))
                {
                    if (!BangNhau(tenPhuongTien, tieuChi.PhuongTien))
                    {
                        continue;
                    }

                    diem += 5;
                    lyDo.Add("Đúng phương tiện di chuyển");
                }

                if (tieuChi.NganSachToiDa.HasValue)
                {
                    if (tour.Gia > tieuChi.NganSachToiDa.Value)
                    {
                        continue;
                    }

                    diem += 5;
                    lyDo.Add("Phù hợp ngân sách");
                }

                if (tieuChi.SoNgay.HasValue)
                {
                    if (tour.SoNgay != tieuChi.SoNgay)
                    {
                        continue;
                    }

                    diem += 5;
                    lyDo.Add("Đúng số ngày");
                }

                if (tieuChi.UuTienTourHot)
                {
                    diem += Math.Min(soLuotDat, 5);
                    lyDo.Add("Tour được nhiều người quan tâm");
                }

                if (diem == 0)
                {
                    diem = soLuotDat > 0 ? soLuotDat : 1;
                }

                ketQua.Add(new TourGoiYItemViewModel
                {
                    Tour = tour,
                    DiemGoiY = diem,
                    SoLuotDat = soLuotDat,
                    LyDoGoiY = lyDo.Any() ? string.Join(", ", lyDo) : "Tour phù hợp để bạn tham khảo",
                    TenDiaDiemHienThi = tour.TenDiaDiemHienThi
                });
            }

            return ketQua
                .OrderByDescending(x => x.DiemGoiY)
                .ThenByDescending(x => x.SoLuotDat)
                .Take(9)
                .ToList();
        }
        // Các phương thức phụ trợ để lấy thông tin loại địa hình, phong cách và loại tour của một tour cụ thể. Chúng giúp việc truy xuất thông tin liên quan đến tour trở nên dễ dàng hơn khi tính điểm gợi ý.
        private List<string> LayLoaiDiaHinh(Tour tour)
        {
            return tour.TourLoaiDiaHinhs
                .Where(x => x.LoaiDiaHinh != null)
                .Select(x => x.LoaiDiaHinh!.TenLoaiDiaHinh)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }

        private List<string> LayPhongCach(Tour tour)
        {
            return tour.TourPhongCachs
                .Where(x => x.PhongCach != null)
                .Select(x => x.PhongCach!.TenPhongCach)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
        }
        // Phương thức này so sánh hai chuỗi một cách không phân biệt hoa thường và bỏ qua khoảng trắng ở đầu và cuối. Nó được sử dụng để kiểm tra xem thông tin của tour có khớp với tiêu chí đã chọn hay không, đặc biệt là khi so sánh loại địa hình, phong cách hoặc phương tiện di chuyển.
        private bool BangNhau(string? a, string? b)
        {
            return string.Equals(
                a?.Trim(),
                b?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );
        }
        // Phương thức này lấy danh sách loại tour của một tour cụ thể. Hiện tại, nó chỉ kiểm tra danh mục của tour để xác định loại tour, nhưng có thể được mở rộng trong tương lai nếu có thêm thông tin về loại tour được lưu trữ trong cơ sở dữ liệu.
        private List<string> LayLoaiTour(Tour tour)
        {
            var ds = new List<string>();

            if (tour.DanhMuc != null && !string.IsNullOrWhiteSpace(tour.DanhMuc.TenDanhMuc))
            {
                ds.Add(tour.DanhMuc.TenDanhMuc);
            }

            return ds;
        }
        // Phương thức này kiểm tra xem một giá trị có khớp với lựa chọn hay không. Nó so sánh hai chuỗi một cách không phân biệt hoa thường và cũng kiểm tra xem một chuỗi có chứa chuỗi kia hay không. Điều này giúp việc so sánh trở nên linh hoạt hơn, đặc biệt khi người dùng có thể nhập các tiêu chí một cách không chính xác hoặc chỉ nhập một phần của thông tin.
        private bool KHopLuaChon(string? giaTri, string? luaChon)
        {
            if (string.IsNullOrWhiteSpace(giaTri) || string.IsNullOrWhiteSpace(luaChon))
            {
                return false;
            }

            giaTri = giaTri.Trim();
            luaChon = luaChon.Trim();

            return giaTri.Equals(luaChon, StringComparison.OrdinalIgnoreCase) ||
                   giaTri.Contains(luaChon, StringComparison.OrdinalIgnoreCase) ||
                   luaChon.Contains(giaTri, StringComparison.OrdinalIgnoreCase);
        }
    }
}