using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Đơn nhóm: trưởng nhóm giữ chỗ có hạn cho cả nhóm, gửi link mời, mỗi thành viên nhận ghế và trả phần mình.
    // Quyết định nghiệp vụ: docs/MO_HINH_DU_LIEU.md, mục 9.
    public class DonNhomController : Controller
    {
        private readonly DuLichDbContext _context;
        private readonly IConfiguration _cauHinh;

        private const string KhoaNhap = "DonNhom_Nhap";

        public DonNhomController(DuLichDbContext context, IConfiguration cauHinh)
        {
            _context = context;
            _cauHinh = cauHinh;
        }

        private int SoGioGiuCho => _cauHinh.GetValue("DonNhom:SoGioGiuCho", 24);

        private int? TaiKhoanDangNhap() => HttpContext.Session.GetInt32("taiKhoanId");

        private bool LaKhach() =>
            TaiKhoanDangNhap().HasValue && HttpContext.Session.GetString("role") is null or "user";

        private LichKhoiHanhTour? LayLich(int tourId, int lichId) =>
            _context.LichKhoiHanhTours
                .Include(l => l.Tour).ThenInclude(t => t!.PhuongTienInfo)
                .Include(l => l.SoDoChoNgoi)
                .FirstOrDefault(l => l.Id == lichId && l.TourId == tourId);

        private TaoDonNhomViewModel? DocNhap()
        {
            var json = HttpContext.Session.GetString(KhoaNhap);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<TaoDonNhomViewModel>(json);
        }

        // Điền phần hiển thị (tour, giá, số chỗ) cho form
        private void DienThongTin(TaoDonNhomViewModel m, LichKhoiHanhTour lich)
        {
            m.TenTour = lich.Tour?.TenTour ?? string.Empty;
            m.HinhAnh = lich.Tour?.HinhAnh;
            m.NgayDi = lich.NgayKhoiHanh;
            m.GioKhoiHanh = lich.GioKhoiHanh;
            m.GiaNguoiLon = lich.GiaNguoiLonHienThi;
            m.GiaTreEm = lich.GiaTreEmHienThi;
            m.SoChoCon = lich.SoChoCon;
            m.CoSoDo = lich.SoDoChoNgoiId != null;
            m.SoGioGiuCho = SoGioGiuCho;
        }

        // ---------- Bước 1: số ghế và thông tin liên hệ ----------
        public IActionResult TaoDon(int id, int lichId)
        {
            if (!TaiKhoanDangNhap().HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            if (!LaKhach())
            {
                return RedirectToAction("Index", "TrangChu");
            }

            var lich = LayLich(id, lichId);
            if (lich == null)
            {
                return NotFound();
            }
            if (lich.Tour == null || lich.Tour.NgungBan || !lich.ChoPhepDatTour)
            {
                TempData["Loi"] = "Lịch khởi hành này không còn nhận đặt tour.";
                return RedirectToAction("ChiTiet", "DiaDiem", new { id });
            }

            var tk = _context.TaiKhoans.AsNoTracking().First(x => x.Id == TaiKhoanDangNhap()!.Value);
            var model = new TaoDonNhomViewModel
            {
                TourId = id,
                LichId = lichId,
                TenKhach = tk.HoTen,
                DienThoai = tk.SoDienThoai ?? string.Empty,
                Email = tk.Email
            };
            DienThongTin(model, lich);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoDon(TaoDonNhomViewModel model)
        {
            if (!TaiKhoanDangNhap().HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var lich = LayLich(model.TourId, model.LichId);
            if (lich == null)
            {
                return NotFound();
            }
            DienThongTin(model, lich);

            int soGhe = model.SoNguoiLon + model.SoTreNho;
            if (lich.Tour == null || lich.Tour.NgungBan || !lich.ChoPhepDatTour)
            {
                ModelState.AddModelError("", "Lịch khởi hành này không còn nhận đặt tour.");
            }
            if (soGhe < 2)
            {
                ModelState.AddModelError("", "Đơn nhóm cần ít nhất 2 ghế. Đi một mình thì dùng Đặt tour thường.");
            }
            if (soGhe > lich.SoChoCon)
            {
                ModelState.AddModelError("", $"Lịch chỉ còn {lich.SoChoCon} chỗ.");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            HttpContext.Session.SetString(KhoaNhap, JsonSerializer.Serialize(model));

            // Lịch có sơ đồ: chọn sẵn chỗ cho nhóm trước khi giữ chỗ
            return lich.SoDoChoNgoiId != null
                ? RedirectToAction("ChonCho")
                : TaoDonTuNhap(model, lich, new List<int?>());
        }

        // ---------- Bước 2: chọn sẵn chỗ cho các ghế (không bắt buộc) ----------
        public IActionResult ChonCho()
        {
            var nhap = DocNhap();
            if (nhap == null || !TaiKhoanDangNhap().HasValue)
            {
                return RedirectToAction("Index", "TrangChu");
            }
            var lich = LayLich(nhap.TourId, nhap.LichId);
            if (lich?.SoDoChoNgoi == null)
            {
                return RedirectToAction("TaoDon", new { id = nhap.TourId, lichId = nhap.LichId });
            }

            var daCoNguoi = _context.ChoNgoiChuyenDis
                .Where(c => c.LichKhoiHanhTourId == lich.Id)
                .Select(c => c.ViTriChoNgoiId)
                .ToHashSet();

            var model = new ChonChoNgoiViewModel
            {
                TenTour = lich.Tour?.TenTour ?? string.Empty,
                NgayDi = lich.NgayKhoiHanh,
                TenSoDo = lich.SoDoChoNgoi.TenSoDo,
                LaMayBay = lich.Tour?.PhuongTienInfo?.TenPhuongTien?.StartsWith("Máy bay") == true,
                SoHang = lich.SoDoChoNgoi.SoHang,
                SoCot = lich.SoDoChoNgoi.SoCot,
                DsViTri = _context.ViTriChoNgois
                    .Where(v => v.SoDoChoNgoiId == lich.SoDoChoNgoi.Id)
                    .OrderBy(v => v.Hang).ThenBy(v => v.Cot)
                    .Select(v => new ViTriHienThi { Id = v.Id, MaViTri = v.MaViTri, Hang = v.Hang, Cot = v.Cot })
                    .ToList(),
                DsHanhKhach = DanhSachGheNhap(nhap)
                    .Select((loai, i) => new HanhKhachChonCho
                    {
                        ThuTu = i,
                        HoTen = i == 0 && nhap.ToiDi ? $"Ghế {i + 1} (bạn)" : $"Ghế {i + 1}",
                        LoaiHanhKhach = loai
                    }).ToList(),
                NhanKhongChon = "Thành viên tự chọn"
            };
            foreach (var v in model.DsViTri)
            {
                v.DaCoNguoi = daCoNguoi.Contains(v.Id);
            }

            ViewBag.QuayLai = Url.Action("TaoDon", new { id = nhap.TourId, lichId = nhap.LichId });
            ViewBag.SoGioGiuCho = SoGioGiuCho;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChonCho(List<int?> viTri)
        {
            var nhap = DocNhap();
            if (nhap == null || !TaiKhoanDangNhap().HasValue)
            {
                return RedirectToAction("Index", "TrangChu");
            }
            var lich = LayLich(nhap.TourId, nhap.LichId);
            if (lich == null)
            {
                return NotFound();
            }
            return TaoDonTuNhap(nhap, lich, viTri ?? new List<int?>());
        }

        // Loại hành khách của từng ghế theo thứ tự: người lớn trước, trẻ nhỏ sau
        private static List<string> DanhSachGheNhap(TaoDonNhomViewModel nhap) =>
            Enumerable.Repeat("Người lớn", nhap.SoNguoiLon)
                .Concat(Enumerable.Repeat("Trẻ nhỏ", nhap.SoTreNho))
                .ToList();

        // Giữ chỗ: trừ chỗ có điều kiện, tạo đơn nhóm, các ghế (ghế đầu của trưởng nhóm nếu đi cùng),
        // mã thanh toán riêng từng ghế và chỗ đã chọn sẵn, tất cả trong một giao dịch CSDL (F1, F2).
        private IActionResult TaoDonTuNhap(TaoDonNhomViewModel nhap, LichKhoiHanhTour lich, List<int?> viTri)
        {
            int taiKhoanId = TaiKhoanDangNhap()!.Value;
            var dsLoai = DanhSachGheNhap(nhap);
            int soGhe = dsLoai.Count;

            if (lich.Tour == null || lich.Tour.NgungBan || !lich.ChoPhepDatTour)
            {
                TempData["Loi"] = "Lịch khởi hành này không còn nhận đặt tour.";
                return RedirectToAction("ChiTiet", "DiaDiem", new { id = nhap.TourId });
            }

            var maChuyenDi = XuLyDon.TaoMaChuyenDi(_context);
            var don = new DatTours
            {
                LichKhoiHanhTourId = lich.Id,
                TaiKhoanId = taiKhoanId,
                MaChuyenDi = maChuyenDi,
                LoaiDon = "Nhom",
                TrangThai = "GiuCho",
                MaMoi = XuLyDon.TaoMaMoi(_context),
                HanGiuCho = DateTime.Now.AddHours(SoGioGiuCho),
                NgayDat = DateTime.Now,
                TenKhach = nhap.TenKhach.Trim(),
                DienThoai = nhap.DienThoai.Trim(),
                Email = nhap.Email?.Trim() ?? string.Empty,
                DiaChi = string.Empty,
                GhiChu = nhap.GhiChu
            };
            for (int i = 0; i < soGhe; i++)
            {
                don.HanhKhachDatTours.Add(new HanhKhachDatTour
                {
                    LoaiHanhKhach = dsLoai[i],
                    GiaLucDat = XuLyDon.GiaGhe(dsLoai[i], lich.GiaNguoiLonHienThi, lich.GiaTreEmHienThi),
                    MaThanhToan = $"{maChuyenDi}-{i + 1}",
                    TaiKhoanId = i == 0 && nhap.ToiDi ? taiKhoanId : null
                });
            }

            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                int soDong = _context.Database.ExecuteSqlInterpolated(
                    $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon - {soGhe} WHERE Id = {lich.Id} AND SoChoCon >= {soGhe} AND TrangThai = {TrangThaiLich.MoBan}");
                if (soDong == 0)
                {
                    giaoDichCsdl.Rollback();
                    TempData["Loi"] = "Lịch khởi hành vừa hết chỗ cho số ghế này, vui lòng giảm số ghế hoặc chọn lịch khác.";
                    return RedirectToAction("TaoDon", new { id = nhap.TourId, lichId = nhap.LichId });
                }

                _context.DatTours.Add(don);
                _context.SaveChanges();

                // Chỗ chọn sẵn: chỉ nhận chỗ thuộc sơ đồ của lịch, không trùng (RB-18); để trống thì thành viên tự chọn
                if (lich.SoDoChoNgoiId.HasValue && viTri.Any(v => v.HasValue))
                {
                    var hopLe = _context.ViTriChoNgois.Where(v => v.SoDoChoNgoiId == lich.SoDoChoNgoiId.Value).Select(v => v.Id).ToHashSet();
                    var dsGhe = don.HanhKhachDatTours.ToList();
                    var daDung = new HashSet<int>();
                    for (int i = 0; i < dsGhe.Count && i < viTri.Count; i++)
                    {
                        if (viTri[i].HasValue && hopLe.Contains(viTri[i]!.Value) && daDung.Add(viTri[i]!.Value))
                        {
                            _context.ChoNgoiChuyenDis.Add(new ChoNgoiChuyenDi
                            {
                                LichKhoiHanhTourId = lich.Id,
                                ViTriChoNgoiId = viTri[i]!.Value,
                                HanhKhachDatTourId = dsGhe[i].Id
                            });
                        }
                    }
                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateException)
                    {
                        giaoDichCsdl.Rollback();
                        TempData["Loi"] = "Có chỗ bạn chọn vừa được người khác đặt. Bạn chọn lại nhé.";
                        return RedirectToAction("ChonCho");
                    }
                }

                giaoDichCsdl.Commit();
            }

            HttpContext.Session.Remove(KhoaNhap);
            TempData["ThanhCong"] = $"Đã giữ {soGhe} chỗ trong {SoGioGiuCho} giờ. Gửi link mời cho các thành viên để họ nhận ghế và trả phần mình.";
            return RedirectToAction("ChiTiet", new { id = don.Id });
        }

        // ---------- Trang đơn nhóm của trưởng nhóm ----------
        public IActionResult ChiTiet(int id)
        {
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var datTour = _context.DatTours.AsNoTracking().FirstOrDefault(x => x.Id == id && x.LoaiDon == "Nhom");
            // RB-21: chỉ trưởng nhóm xem được trang quản lý đơn
            if (datTour == null || datTour.TaiKhoanId != taiKhoanId.Value)
            {
                return NotFound();
            }

            var don = _context.DatTours.AsNoTracking().Where(x => x.Id == id).ChonDon(_context).First();
            var dsGhe = _context.HanhKhachDatTours.AsNoTracking().Include(h => h.TaiKhoan)
                .Where(h => h.DatTourId == id).OrderBy(h => h.Id).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            var dsGiaoDich = _context.GiaoDichThanhToans.AsNoTracking().Where(g => g.HanhKhachDatTourId != null && dsGheId.Contains(g.HanhKhachDatTourId.Value)).ToList();
            var choTheoGhe = _context.ChoNgoiChuyenDis.AsNoTracking()
                .Where(c => dsGheId.Contains(c.HanhKhachDatTourId))
                .Select(c => new { c.HanhKhachDatTourId, c.ViTriChoNgoi!.MaViTri })
                .ToDictionary(c => c.HanhKhachDatTourId, c => c.MaViTri);

            var model = new DonNhomChiTietViewModel
            {
                Don = don,
                HanGiuCho = datTour.HanGiuCho,
                DaGiaHan = datTour.DaGiaHan,
                LinkMoi = datTour.MaMoi != null && !don.DaHuy
                    ? Url.Action("Moi", "DonNhom", new { ma = datTour.MaMoi }, Request.Scheme)
                    : null,
                DsGhe = dsGhe.Select((h, i) => new GheNhomHienThi
                {
                    Id = h.Id,
                    ThuTu = i + 1,
                    LoaiHanhKhach = h.LoaiHanhKhach,
                    GiaLucDat = h.GiaLucDat,
                    MaThanhToan = h.MaThanhToan,
                    // Chỉ hiện tên, không hiện giấy tờ của người khác (RB-20)
                    TenChu = !string.IsNullOrWhiteSpace(h.HoTen) ? h.HoTen : h.TaiKhoan?.HoTen,
                    LaCuaToi = h.TaiKhoanId == taiKhoanId.Value,
                    DaCoChu = h.TaiKhoanId.HasValue || !string.IsNullOrWhiteSpace(h.HoTen),
                    CoTaiKhoan = h.TaiKhoanId.HasValue,
                    MaCho = choTheoGhe.TryGetValue(h.Id, out var ma) ? ma : null,
                    TrangThai = TrangThaiGhe.Tinh(h, dsGiaoDich)
                }).ToList()
            };
            return View(model);
        }

        // ---------- Link mời: xem không cần đăng nhập, nhận ghế phải đăng nhập (N3) ----------

        // Đơn còn nhận người: đang giữ chỗ, chưa quá hạn, lịch chưa hủy và chưa tới ngày đi
        private static bool DonConNhanGhe(DatTours don) =>
            don.LoaiDon == "Nhom"
            && don.TrangThai == "GiuCho"
            && don.HanGiuCho > DateTime.Now
            && don.LichKhoiHanhTour != null
            && don.LichKhoiHanhTour.TrangThai != TrangThaiLich.DaHuy
            && don.LichKhoiHanhTour.NgayKhoiHanh.Date > DateTime.Today;

        private DatTours? LayDonTheoMaMoi(string? ma) =>
            string.IsNullOrEmpty(ma)
                ? null
                : _context.DatTours.AsNoTracking()
                    .Include(x => x.LichKhoiHanhTour).ThenInclude(l => l!.Tour)
                    .FirstOrDefault(x => x.MaMoi == ma && x.LoaiDon == "Nhom");

        public IActionResult Moi(string? ma)
        {
            var don = LayDonTheoMaMoi(ma);
            // Link sai, đã đổi mã, đơn đã chốt, hủy hoặc quá hạn: báo dễ hiểu thay vì lỗi 404
            if (don == null || !DonConNhanGhe(don))
            {
                return View("LinkHetHan");
            }

            var taiKhoanId = TaiKhoanDangNhap();
            var dsGhe = _context.HanhKhachDatTours.AsNoTracking()
                .Where(h => h.DatTourId == don.Id).OrderBy(h => h.Id).ToList();

            // Đã có ghế trong đơn thì vào thẳng trang ghế của mình
            var gheCuaToi = taiKhoanId.HasValue ? dsGhe.FirstOrDefault(h => h.TaiKhoanId == taiKhoanId.Value && h.NgayNha == null) : null;
            if (gheCuaToi != null)
            {
                return RedirectToAction("GheCuaToi", new { id = gheCuaToi.Id });
            }

            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            var choTheoGhe = _context.ChoNgoiChuyenDis.AsNoTracking()
                .Where(c => dsGheId.Contains(c.HanhKhachDatTourId))
                .Select(c => new { c.HanhKhachDatTourId, c.ViTriChoNgoi!.MaViTri })
                .ToDictionary(c => c.HanhKhachDatTourId, c => c.MaViTri);
            var lich = don.LichKhoiHanhTour!;

            var model = new MoiNhomViewModel
            {
                Ma = ma!,
                DonId = don.Id,
                MaChuyenDi = don.MaChuyenDi,
                TenTour = lich.Tour?.TenTour ?? string.Empty,
                HinhAnh = lich.Tour?.HinhAnh,
                NgayDi = lich.NgayKhoiHanh,
                GioKhoiHanh = lich.GioKhoiHanh,
                TenTruongNhom = don.TenKhach,
                HanGiuCho = don.HanGiuCho!.Value,
                DaGiaHan = don.DaGiaHan,
                DaDangNhap = taiKhoanId.HasValue,
                LaTruongNhom = taiKhoanId.HasValue && don.TaiKhoanId == taiKhoanId.Value,
                // Số thứ tự đếm cả ghế đã nhả để khớp với trang của trưởng nhóm
                DsGhe = dsGhe.Select((h, i) => new { h, i })
                    .Where(x => x.h.NgayNha == null)
                    .Select(x => new GheMoiHienThi
                    {
                        Id = x.h.Id,
                        ThuTu = x.i + 1,
                        LoaiHanhKhach = x.h.LoaiHanhKhach,
                        GiaLucDat = x.h.GiaLucDat,
                        MaCho = choTheoGhe.TryGetValue(x.h.Id, out var maCho) ? maCho : null,
                        ConTrong = !x.h.TaiKhoanId.HasValue && string.IsNullOrWhiteSpace(x.h.HoTen)
                    }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NhanGhe(string ma, int gheId)
        {
            var quayLai = Url.Action("Moi", new { ma })!;
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                // Đăng nhập xong quay lại đúng link mời
                return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = quayLai });
            }
            if (!LaKhach())
            {
                TempData["Loi"] = "Tài khoản quản trị hoặc nhân viên không nhận ghế trong đơn nhóm được.";
                return Redirect(quayLai);
            }

            var don = LayDonTheoMaMoi(ma);
            if (don == null || !DonConNhanGhe(don))
            {
                return View("LinkHetHan");
            }

            // Nhận ghế bằng một câu UPDATE có điều kiện "ghế còn trống": hai người bấm cùng lúc thì chỉ một người được.
            // Ghế trẻ nhỏ không nhận qua link; mỗi tài khoản một ghế trong một đơn (RB-03, có chỉ mục duy nhất chặn thêm).
            int soDong;
            try
            {
                soDong = _context.Database.ExecuteSqlInterpolated($@"
                    UPDATE HanhKhachDatTours SET TaiKhoanId = {taiKhoanId.Value}
                    WHERE Id = {gheId} AND DatTourId = {don.Id} AND NgayNha IS NULL
                      AND TaiKhoanId IS NULL AND (HoTen IS NULL OR HoTen = '')
                      AND LoaiHanhKhach <> {"Trẻ nhỏ"}
                      AND NOT EXISTS (SELECT 1 FROM HanhKhachDatTours WHERE DatTourId = {don.Id} AND TaiKhoanId = {taiKhoanId.Value})");
            }
            catch (Microsoft.Data.SqlClient.SqlException)
            {
                soDong = 0;
            }

            if (soDong == 0)
            {
                TempData["Loi"] = "Ghế này vừa có người nhận hoặc bạn đã có ghế trong đơn. Bạn xem lại danh sách nhé.";
                return Redirect(quayLai);
            }

            TempData["ThanhCong"] = "Bạn đã nhận ghế. Điền thông tin hành khách để hoàn tất phần của mình.";
            return RedirectToAction("GheCuaToi", new { id = gheId });
        }

        // ---------- Một ghế: thông tin, chỗ ngồi, trả lại ghế ----------

        // Ghế người đang đăng nhập được mở: ghế của chính họ, hoặc ghế chưa có tài khoản trong đơn họ là trưởng nhóm.
        // Trưởng nhóm không mở được ghế người khác đã nhận, nên không thấy giấy tờ của họ (RB-20).
        private HanhKhachDatTour? LayGheDuocMo(int gheId, int taiKhoanId, out bool laTruongNhom)
        {
            var ghe = _context.HanhKhachDatTours
                .Include(h => h.DatTour).ThenInclude(d => d!.LichKhoiHanhTour).ThenInclude(l => l!.Tour).ThenInclude(t => t!.PhuongTienInfo)
                .Include(h => h.DatTour).ThenInclude(d => d!.LichKhoiHanhTour).ThenInclude(l => l!.SoDoChoNgoi)
                .Include(h => h.TaiKhoan)
                .FirstOrDefault(h => h.Id == gheId && h.DatTour!.LoaiDon == "Nhom");

            laTruongNhom = ghe?.DatTour?.TaiKhoanId == taiKhoanId;
            if (ghe == null || ghe.NgayNha.HasValue)
            {
                return null;
            }
            if (ghe.TaiKhoanId == taiKhoanId)
            {
                return ghe;
            }
            return laTruongNhom && !ghe.TaiKhoanId.HasValue ? ghe : null;
        }

        private string TinhTrangThaiGhe(HanhKhachDatTour ghe) =>
            TrangThaiGhe.Tinh(ghe, _context.GiaoDichThanhToans.AsNoTracking().Where(g => g.HanhKhachDatTourId == ghe.Id).ToList());

        // Thông tin sửa được khi đơn còn giữ chỗ và ghế chưa trả, chưa chờ xác nhận
        private static bool GheDuocSua(DatTours don, string trangThai) =>
            DonConNhanGhe(don) && trangThai != TrangThaiGhe.DaTra && trangThai != TrangThaiGhe.ChoXacNhan;

        // Chỗ ngồi đổi được tới trước ngày đi, khi đơn chưa hủy và lịch có sơ đồ
        private static bool GheDuocDoiCho(DatTours don) =>
            don.TrangThai != "DaHuy"
            && don.LichKhoiHanhTour?.SoDoChoNgoiId != null
            && don.LichKhoiHanhTour.TrangThai != TrangThaiLich.DaHuy
            && don.LichKhoiHanhTour.NgayKhoiHanh.Date > DateTime.Today;

        // Trả được khi đơn còn giữ chỗ và ghế đã điền đủ thông tin, chưa có giao dịch chờ hoặc thành công.
        // Thông tin khóa lại sau khi báo chuyển khoản nên phải điền trước (N4).
        private static bool GheDuocTra(DatTours don, string trangThai) =>
            DonConNhanGhe(don) && trangThai == TrangThaiGhe.DaDien;

        private TraTienGheViewModel TaoTraTien(HanhKhachDatTour ghe, DatTours don, string trangThai, bool traHo)
        {
            bool duocTra = GheDuocTra(don, trangThai);
            return new TraTienGheViewModel
            {
                GheId = ghe.Id,
                TrangThai = trangThai,
                GiaLucDat = ghe.GiaLucDat,
                LoaiHanhKhach = ghe.LoaiHanhKhach,
                MaThanhToan = ghe.MaThanhToan,
                DuocTra = duocTra,
                TraHo = traHo,
                ChuyenKhoan = duocTra && ghe.MaThanhToan != null
                    ? ThongTinChuyenKhoan.Tao(_cauHinh, ghe.GiaLucDat, ghe.MaThanhToan)
                    : null
            };
        }

        private GheCuaToiViewModel TaoTrangGhe(HanhKhachDatTour ghe, int taiKhoanId, bool laTruongNhom, ThongTinGheForm? form = null)
        {
            var don = ghe.DatTour!;
            var lich = don.LichKhoiHanhTour!;
            var trangThai = TinhTrangThaiGhe(ghe);
            int thuTu = _context.HanhKhachDatTours.Count(h => h.DatTourId == don.Id && h.Id < ghe.Id) + 1;
            var maCho = _context.ChoNgoiChuyenDis.Where(c => c.HanhKhachDatTourId == ghe.Id).Select(c => c.ViTriChoNgoi!.MaViTri).FirstOrDefault();
            bool laTreNho = ghe.LoaiHanhKhach == "Trẻ nhỏ";

            return new GheCuaToiViewModel
            {
                GheId = ghe.Id,
                DonId = don.Id,
                ThuTu = thuTu,
                MaChuyenDi = don.MaChuyenDi,
                MaThanhToan = ghe.MaThanhToan,
                MaMoi = DonConNhanGhe(don) ? don.MaMoi : null,
                TenTour = lich.Tour?.TenTour ?? string.Empty,
                NgayDi = lich.NgayKhoiHanh,
                GioKhoiHanh = lich.GioKhoiHanh,
                HanGiuCho = don.TrangThai == "GiuCho" ? don.HanGiuCho : null,
                LoaiHanhKhach = ghe.LoaiHanhKhach,
                GiaLucDat = ghe.GiaLucDat,
                MaCho = maCho,
                TrangThai = trangThai,
                DienHo = ghe.TaiKhoanId != taiKhoanId,
                LaTruongNhom = laTruongNhom,
                DuocSua = GheDuocSua(don, trangThai),
                DuocDoiCho = GheDuocDoiCho(don),
                CoSoDo = lich.SoDoChoNgoiId != null,
                DaDien = !string.IsNullOrWhiteSpace(ghe.HoTen),
                TraTien = TaoTraTien(ghe, don, trangThai, traHo: ghe.TaiKhoanId != taiKhoanId),
                Form = form ?? new ThongTinGheForm
                {
                    // Chủ ghế chưa điền thì gợi ý sẵn họ tên tài khoản
                    HoTen = ghe.HoTen ?? (ghe.TaiKhoanId == taiKhoanId ? ghe.TaiKhoan?.HoTen ?? string.Empty : string.Empty),
                    LoaiGiayTo = ghe.LoaiGiayTo ?? (laTreNho ? "Giấy khai sinh" : "CCCD"),
                    SoGiayTo = ghe.SoGiayTo ?? string.Empty
                }
            };
        }

        public IActionResult GheCuaToi(int id)
        {
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = Url.Action("GheCuaToi", new { id }) });
            }

            var ghe = LayGheDuocMo(id, taiKhoanId.Value, out var laTruongNhom);
            if (ghe == null)
            {
                return NotFound();
            }
            return View(TaoTrangGhe(ghe, taiKhoanId.Value, laTruongNhom));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThongTinGhe(int id, [Bind(Prefix = "Form")] ThongTinGheForm form)
        {
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ghe = LayGheDuocMo(id, taiKhoanId.Value, out var laTruongNhom);
            if (ghe == null)
            {
                return NotFound();
            }

            if (!GheDuocSua(ghe.DatTour!, TinhTrangThaiGhe(ghe)))
            {
                TempData["Loi"] = "Ghế đã báo chuyển khoản, đã trả, hoặc đơn không còn giữ chỗ, không sửa thông tin được nữa.";
                return RedirectToAction("GheCuaToi", new { id });
            }
            if (!ThongTinGheForm.DsLoaiGiayTo.Contains(form.LoaiGiayTo))
            {
                ModelState.AddModelError("Form.LoaiGiayTo", "Loại giấy tờ không hợp lệ.");
            }
            if (!ModelState.IsValid)
            {
                return View("GheCuaToi", TaoTrangGhe(ghe, taiKhoanId.Value, laTruongNhom, form));
            }

            // Ghi có điều kiện: trưởng nhóm điền hộ chỉ thành công khi ghế vẫn chưa có ai nhận
            int soDong = _context.Database.ExecuteSqlInterpolated($@"
                UPDATE HanhKhachDatTours SET HoTen = {form.HoTen.Trim()}, LoaiGiayTo = {form.LoaiGiayTo}, SoGiayTo = {form.SoGiayTo.Trim()}
                WHERE Id = {id} AND NgayNha IS NULL
                  AND (TaiKhoanId = {taiKhoanId.Value} OR (TaiKhoanId IS NULL AND {(laTruongNhom ? 1 : 0)} = 1))
                  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans WHERE HanhKhachDatTourId = {id} AND TrangThai IN ('ChoXacNhan', 'ThanhCong'))");

            if (soDong == 0)
            {
                TempData["Loi"] = "Ghế vừa có thành viên nhận hoặc vừa được báo chuyển khoản, thông tin chưa được lưu.";
                return RedirectToAction("ChiTiet", new { id = ghe.DatTourId });
            }

            TempData["ThanhCong"] = "Đã lưu thông tin hành khách.";
            return ghe.TaiKhoanId == taiKhoanId.Value
                ? RedirectToAction("GheCuaToi", new { id })
                : RedirectToAction("ChiTiet", new { id = ghe.DatTourId });
        }

        // Trả lại ghế khi chưa trả tiền: ghế về trống, xóa thông tin, giữ chỗ ngồi đã chọn để người sau nhận
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TraLaiGhe(int id)
        {
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ghe = LayGheDuocMo(id, taiKhoanId.Value, out var laTruongNhom);
            if (ghe == null)
            {
                return NotFound();
            }
            var don = ghe.DatTour!;
            if (!GheDuocSua(don, TinhTrangThaiGhe(ghe)))
            {
                TempData["Loi"] = "Ghế đã báo chuyển khoản, đã trả, hoặc đơn không còn giữ chỗ, không trả lại ghế được.";
                return RedirectToAction("GheCuaToi", new { id });
            }

            // Không trả lại được ghế vừa được báo chuyển khoản (kể cả trưởng nhóm trả hộ cùng lúc)
            int soDong = _context.Database.ExecuteSqlInterpolated($@"
                UPDATE HanhKhachDatTours SET TaiKhoanId = NULL, HoTen = NULL, LoaiGiayTo = NULL, SoGiayTo = NULL
                WHERE Id = {id} AND NgayNha IS NULL
                  AND (TaiKhoanId = {taiKhoanId.Value} OR (TaiKhoanId IS NULL AND {(laTruongNhom ? 1 : 0)} = 1))
                  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans WHERE HanhKhachDatTourId = {id} AND TrangThai IN ('ChoXacNhan', 'ThanhCong'))");
            if (soDong == 0)
            {
                TempData["Loi"] = "Ghế vừa được báo chuyển khoản nên không trả lại được.";
                return RedirectToAction("GheCuaToi", new { id });
            }

            TempData["ThanhCong"] = "Ghế đã trở về trống, người khác có thể nhận qua link mời.";
            if (laTruongNhom)
            {
                return RedirectToAction("ChiTiet", new { id = don.Id });
            }
            return RedirectToAction("Moi", new { ma = don.MaMoi });
        }

        // ---------- Trả tiền theo ghế: QR riêng, báo đã chuyển khoản, trưởng nhóm trả hộ (N4) ----------

        // Ghế người đang đăng nhập được trả tiền: ghế của mình, hoặc bất kỳ ghế nào trong đơn mình là trưởng nhóm
        private HanhKhachDatTour? LayGheDuocTra(int gheId, int taiKhoanId)
        {
            var ghe = _context.HanhKhachDatTours
                .Include(h => h.DatTour).ThenInclude(d => d!.LichKhoiHanhTour).ThenInclude(l => l!.Tour)
                .Include(h => h.TaiKhoan)
                .FirstOrDefault(h => h.Id == gheId && h.DatTour!.LoaiDon == "Nhom" && h.NgayNha == null);
            if (ghe == null)
            {
                return null;
            }
            return ghe.TaiKhoanId == taiKhoanId || ghe.DatTour!.TaiKhoanId == taiKhoanId ? ghe : null;
        }

        // Trang trả hộ cho ghế thành viên khác đã nhận: chỉ có tên, giá và QR, không có giấy tờ (RB-20)
        public IActionResult TraHo(int id)
        {
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan", new { returnUrl = Url.Action("TraHo", new { id }) });
            }

            var ghe = LayGheDuocTra(id, taiKhoanId.Value);
            if (ghe == null || ghe.DatTour!.TaiKhoanId != taiKhoanId.Value)
            {
                return NotFound();
            }
            // Ghế của mình hoặc ghế không có tài khoản: trang ghế đầy đủ đã có khung trả tiền
            if (ghe.TaiKhoanId == taiKhoanId.Value || !ghe.TaiKhoanId.HasValue)
            {
                return RedirectToAction("GheCuaToi", new { id });
            }

            var don = ghe.DatTour;
            return View(new TraHoViewModel
            {
                DonId = don.Id,
                MaChuyenDi = don.MaChuyenDi,
                TenTour = don.LichKhoiHanhTour?.Tour?.TenTour ?? string.Empty,
                ThuTu = _context.HanhKhachDatTours.Count(h => h.DatTourId == don.Id && h.Id < ghe.Id) + 1,
                TenChu = !string.IsNullOrWhiteSpace(ghe.HoTen) ? ghe.HoTen : ghe.TaiKhoan?.HoTen,
                TraTien = TaoTraTien(ghe, don, TinhTrangThaiGhe(ghe), traHo: true)
            });
        }

        // Người trả bấm "Tôi đã chuyển khoản": tạo giao dịch chờ xác nhận gắn với ghế, ghi ai trả (NguoiTraId).
        // Một câu INSERT có điều kiện: ghế còn hiệu lực, đã điền đủ, chưa có giao dịch chờ hoặc thành công,
        // nên bấm hai lần hoặc chủ ghế và trưởng nhóm cùng bấm thì chỉ tạo một giao dịch (RB-26).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DaChuyenKhoan(int id, string? phuongThuc)
        {
            // Chỉ nhận hai giá trị đã biết, giá trị lạ coi như chuyển khoản ngân hàng
            phuongThuc = phuongThuc == ThongTinChuyenKhoan.Momo ? ThongTinChuyenKhoan.Momo : ThongTinChuyenKhoan.NganHang;
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ghe = LayGheDuocTra(id, taiKhoanId.Value);
            if (ghe == null)
            {
                return NotFound();
            }
            bool laChuGhe = ghe.TaiKhoanId == taiKhoanId.Value;
            IActionResult QuayLai() => laChuGhe
                ? RedirectToAction("GheCuaToi", new { id })
                : RedirectToAction("ChiTiet", new { id = ghe.DatTourId });

            if (!GheDuocTra(ghe.DatTour!, TinhTrangThaiGhe(ghe)))
            {
                TempData["Loi"] = "Ghế chưa điền đủ thông tin, đã báo chuyển khoản rồi, hoặc đơn không còn giữ chỗ.";
                return QuayLai();
            }

            int soDong = _context.Database.ExecuteSqlInterpolated($@"
                INSERT INTO GiaoDichThanhToans (HanhKhachDatTourId, NguoiTraId, SoTien, PhuongThuc, TrangThai, NgayTao)
                SELECT h.Id, {taiKhoanId.Value}, h.GiaLucDat, {phuongThuc}, 'ChoXacNhan', {DateTime.Now}
                FROM HanhKhachDatTours h
                WHERE h.Id = {id} AND h.NgayNha IS NULL
                  AND h.HoTen IS NOT NULL AND h.HoTen <> '' AND h.SoGiayTo IS NOT NULL AND h.SoGiayTo <> ''
                  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans g WHERE g.HanhKhachDatTourId = h.Id AND g.TrangThai IN ('ChoXacNhan', 'ThanhCong'))");

            if (soDong == 0)
            {
                TempData["Loi"] = "Ghế này vừa được báo chuyển khoản hoặc thông tin vừa bị xóa. Bạn xem lại nhé.";
                return QuayLai();
            }

            TempData["ThanhCong"] = laChuGhe
                ? "Đã ghi nhận bạn chuyển khoản. TravelVN sẽ đối chiếu sao kê và xác nhận, thường trong giờ làm việc."
                : $"Đã ghi nhận bạn trả hộ ghế của {ghe.HoTen}. TravelVN sẽ đối chiếu sao kê và xác nhận.";
            return QuayLai();
        }

        public IActionResult ChoNgoiGhe(int id)
        {
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ghe = LayGheDuocMo(id, taiKhoanId.Value, out _);
            if (ghe == null)
            {
                return NotFound();
            }
            var don = ghe.DatTour!;
            if (!GheDuocDoiCho(don))
            {
                TempData["Loi"] = "Chuyến này không chọn chỗ được nữa.";
                return RedirectToAction("GheCuaToi", new { id });
            }

            var lich = don.LichKhoiHanhTour!;
            var choHienTai = _context.ChoNgoiChuyenDis.Where(c => c.HanhKhachDatTourId == ghe.Id).Select(c => (int?)c.ViTriChoNgoiId).FirstOrDefault();
            // Chỗ của chính ghế này vẫn hiện là chọn được
            var cuaNguoiKhac = _context.ChoNgoiChuyenDis
                .Where(c => c.LichKhoiHanhTourId == lich.Id && c.HanhKhachDatTourId != ghe.Id)
                .Select(c => c.ViTriChoNgoiId)
                .ToHashSet();

            var model = new ChonChoNgoiViewModel
            {
                TenTour = lich.Tour?.TenTour ?? string.Empty,
                NgayDi = lich.NgayKhoiHanh,
                TenSoDo = lich.SoDoChoNgoi!.TenSoDo,
                LaMayBay = lich.Tour?.PhuongTienInfo?.TenPhuongTien?.StartsWith("Máy bay") == true,
                SoHang = lich.SoDoChoNgoi.SoHang,
                SoCot = lich.SoDoChoNgoi.SoCot,
                DsViTri = _context.ViTriChoNgois
                    .Where(v => v.SoDoChoNgoiId == lich.SoDoChoNgoi.Id)
                    .OrderBy(v => v.Hang).ThenBy(v => v.Cot)
                    .Select(v => new ViTriHienThi { Id = v.Id, MaViTri = v.MaViTri, Hang = v.Hang, Cot = v.Cot })
                    .ToList(),
                DsHanhKhach = new List<HanhKhachChonCho>
                {
                    new HanhKhachChonCho
                    {
                        ThuTu = 0,
                        HoTen = !string.IsNullOrWhiteSpace(ghe.HoTen) ? ghe.HoTen : "Ghế của bạn",
                        LoaiHanhKhach = ghe.LoaiHanhKhach,
                        ViTriId = choHienTai
                    }
                },
                DatTourId = ghe.Id,
                ChoTuDongXep = false
            };
            foreach (var v in model.DsViTri)
            {
                v.DaCoNguoi = cuaNguoiKhac.Contains(v.Id);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChoNgoiGhe(int id, List<int?> viTri)
        {
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ghe = LayGheDuocMo(id, taiKhoanId.Value, out _);
            if (ghe == null)
            {
                return NotFound();
            }
            var don = ghe.DatTour!;
            if (!GheDuocDoiCho(don))
            {
                TempData["Loi"] = "Chuyến này không chọn chỗ được nữa.";
                return RedirectToAction("GheCuaToi", new { id });
            }

            int? chon = viTri != null && viTri.Count > 0 ? viTri[0] : null;
            // Ô để trống: giữ chỗ hiện tại. RB-18: chỗ phải thuộc sơ đồ của lịch.
            if (chon.HasValue && _context.ViTriChoNgois.Any(v => v.Id == chon.Value && v.SoDoChoNgoiId == don.LichKhoiHanhTour!.SoDoChoNgoiId))
            {
                using var giaoDichCsdl = _context.Database.BeginTransaction();
                _context.ChoNgoiChuyenDis.RemoveRange(_context.ChoNgoiChuyenDis.Where(c => c.HanhKhachDatTourId == ghe.Id));
                _context.SaveChanges();
                _context.ChoNgoiChuyenDis.Add(new ChoNgoiChuyenDi
                {
                    LichKhoiHanhTourId = don.LichKhoiHanhTourId,
                    ViTriChoNgoiId = chon.Value,
                    HanhKhachDatTourId = ghe.Id
                });
                try
                {
                    _context.SaveChanges();
                    giaoDichCsdl.Commit();
                }
                catch (DbUpdateException)
                {
                    giaoDichCsdl.Rollback();
                    TempData["Loi"] = "Chỗ bạn chọn vừa có người đặt, chỗ cũ vẫn giữ nguyên. Bạn chọn lại nhé.";
                    return RedirectToAction("ChoNgoiGhe", new { id });
                }
                TempData["ThanhCong"] = "Đã lưu chỗ ngồi.";
            }

            return ghe.TaiKhoanId == taiKhoanId.Value
                ? RedirectToAction("GheCuaToi", new { id })
                : RedirectToAction("ChiTiet", new { id = don.Id });
        }
    }
}
