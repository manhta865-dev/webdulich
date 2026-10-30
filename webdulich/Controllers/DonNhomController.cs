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
                    MaCho = choTheoGhe.TryGetValue(h.Id, out var ma) ? ma : null,
                    TrangThai = TrangThaiGhe.Tinh(h, dsGiaoDich)
                }).ToList()
            };
            return View(model);
        }

        // Trang link mời: làm ở phần N3 (nhận ghế qua link)
        public IActionResult Moi(string ma)
        {
            var don = _context.DatTours.AsNoTracking().FirstOrDefault(x => x.MaMoi == ma && x.LoaiDon == "Nhom");
            if (don == null)
            {
                return NotFound();
            }
            TempData["Loi"] = "Trang nhận ghế qua link mời đang được làm (phần N3).";
            return RedirectToAction("Index", "TrangChu");
        }
    }
}
