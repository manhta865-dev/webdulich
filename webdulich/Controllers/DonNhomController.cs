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

        private int SoGioGiaHan => _cauHinh.GetValue("DonNhom:SoGioGiaHan", 6);

        // Mở trang đơn nhóm thì xử lý hết hạn ngay cho đơn đó, không đợi dịch vụ nền (host có thể đang "ngủ"). N5.
        // Đơn chưa quá hạn chỉ tốn một câu đọc.
        private void XuLyNeuQuaHan(int? donId)
        {
            if (donId.HasValue)
            {
                XuLyDon.XuLyDonQuaHan(_context, donId.Value, SoGioGiaHan);
            }
        }

        private int? DonCuaGhe(int gheId) =>
            _context.HanhKhachDatTours.Where(h => h.Id == gheId).Select(h => (int?)h.DatTourId).FirstOrDefault();

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
            var bayGio = GioVN.BayGio;
            m.HanGiuChoDuKien = XuLyDon.TinhHanGiuCho(bayGio, SoGioGiuCho, lich.NgayKhoiHanh);
            m.HanBiRutNgan = m.HanGiuChoDuKien < bayGio.AddHours(SoGioGiuCho);
        }

        // Ngày đi là hôm nay (hạn giữ chỗ không còn sau bây giờ): không tạo đơn nhóm được
        private const string LoiSatNgay = "Đơn nhóm phải tạo trước ngày khởi hành, để cả nhóm có thời gian nhận ghế và trả tiền. Bạn chọn lịch khác hoặc đặt tour thường nhé.";

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

            if (XuLyDon.TinhHanGiuCho(GioVN.BayGio, SoGioGiuCho, lich.NgayKhoiHanh) <= GioVN.BayGio)
            {
                TempData["Loi"] = LoiSatNgay;
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
            if (model.HanGiuChoDuKien <= GioVN.BayGio)
            {
                ModelState.AddModelError("", LoiSatNgay);
            }
            if (soGhe < 2)
            {
                ModelState.AddModelError("", "Đơn nhóm cần ít nhất 2 ghế. Đi một mình thì dùng Đặt tour thường.");
            }
            if (soGhe > lich.SoChoCon)
            {
                ModelState.AddModelError("", $"Lịch chỉ còn {lich.SoChoCon} chỗ.");
            }
            // Mã khuyến mãi (RB-40): kiểm như đơn cá nhân, tính trước số tiền giảm để hiện ở bước sau
            model.MaKhuyenMai = string.IsNullOrWhiteSpace(model.MaKhuyenMai) ? null : model.MaKhuyenMai.Trim().ToUpper();
            if (model.MaKhuyenMai != null)
            {
                var km = XuLyDon.LayKhuyenMaiHopLe(_context, model.MaKhuyenMai);
                if (km == null)
                {
                    ModelState.AddModelError(nameof(model.MaKhuyenMai), "Mã khuyến mãi không đúng, đã hết hạn hoặc hết lượt.");
                }
                else if (soGhe >= 2)
                {
                    model.TenKhuyenMai = km.TenKhuyenMai;
                    model.TienGiamDuKien = TinhGiamDonNhom(model, lich, km).Sum();
                }
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
                NhanKhongChon = "Để TravelVN xếp"
            };
            var khongMoBan = SucChuaLich.ViTriKhoa(_context, lich.Id);
            foreach (var v in model.DsViTri)
            {
                v.DaCoNguoi = daCoNguoi.Contains(v.Id);
                v.KhongMoBan = khongMoBan.Contains(v.Id);
            }

            ViewBag.QuayLai = Url.Action("TaoDon", new { id = nhap.TourId, lichId = nhap.LichId });
            if (!string.IsNullOrEmpty(nhap.MaKhuyenMai) && nhap.TienGiamDuKien > 0)
            {
                ViewBag.KhuyenMai = $"Mã {nhap.MaKhuyenMai} ({nhap.TenKhuyenMai}): cả nhóm được giảm {nhap.TienGiamDuKien:N0} VNĐ, chia cho từng ghế khi giữ chỗ.";
            }
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

        // Phần giảm của từng ghế (cùng thứ tự với DanhSachGheNhap) khi đơn nhóm dùng mã km (RB-40)
        private static List<decimal> TinhGiamDonNhom(TaoDonNhomViewModel nhap, LichKhoiHanhTour lich, KhuyenMai km)
        {
            var dsGia = DanhSachGheNhap(nhap)
                .Select(loai => XuLyDon.GiaGhe(loai, lich.GiaNguoiLonHienThi, lich.GiaTreEmHienThi))
                .ToList();
            return XuLyDon.ChiaTienGiam(dsGia, XuLyDon.TinhTienGiam(km, dsGia.Sum()));
        }

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
            var hanGiuCho = XuLyDon.TinhHanGiuCho(GioVN.BayGio, SoGioGiuCho, lich.NgayKhoiHanh);
            if (hanGiuCho <= GioVN.BayGio)
            {
                TempData["Loi"] = LoiSatNgay;
                return RedirectToAction("ChiTiet", "DiaDiem", new { id = nhap.TourId });
            }

            // Kiểm lại mã lúc giữ chỗ: mã có thể vừa hết hạn hoặc hết lượt sau bước nhập (RB-40)
            KhuyenMai? khuyenMai = null;
            if (!string.IsNullOrEmpty(nhap.MaKhuyenMai))
            {
                khuyenMai = XuLyDon.LayKhuyenMaiHopLe(_context, nhap.MaKhuyenMai);
                if (khuyenMai == null)
                {
                    TempData["Loi"] = $"Mã khuyến mãi {nhap.MaKhuyenMai} vừa hết hạn hoặc hết lượt. Bạn nhập lại hoặc bỏ mã nhé.";
                    return RedirectToAction("TaoDon", new { id = nhap.TourId, lichId = nhap.LichId });
                }
            }
            var dsGiam = khuyenMai != null ? TinhGiamDonNhom(nhap, lich, khuyenMai) : dsLoai.Select(_ => 0m).ToList();

            var maChuyenDi = XuLyDon.TaoMaChuyenDi(_context);
            var don = new DatTours
            {
                LichKhoiHanhTourId = lich.Id,
                TaiKhoanId = taiKhoanId,
                MaChuyenDi = maChuyenDi,
                LoaiDon = "Nhom",
                TrangThai = "GiuCho",
                MaMoi = XuLyDon.TaoMaMoi(_context),
                HanGiuCho = hanGiuCho,
                NgayDat = GioVN.BayGio,
                TenKhach = nhap.TenKhach.Trim(),
                DienThoai = nhap.DienThoai.Trim(),
                Email = nhap.Email?.Trim() ?? string.Empty,
                DiaChi = string.Empty,
                GhiChu = nhap.GhiChu,
                // Đơn nhóm: tiền giảm lưu ở từng ghế, SoTienGiam của đơn luôn 0 (RB-40)
                KhuyenMaiId = khuyenMai?.Id,
                SoTienGiam = 0
            };
            for (int i = 0; i < soGhe; i++)
            {
                don.HanhKhachDatTours.Add(new HanhKhachDatTour
                {
                    LoaiHanhKhach = dsLoai[i],
                    GiaLucDat = XuLyDon.GiaGhe(dsLoai[i], lich.GiaNguoiLonHienThi, lich.GiaTreEmHienThi),
                    TienGiam = dsGiam[i],
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
                    var hopLe = XepChoNgoi.ViTriChonDuoc(_context, lich.Id, lich.SoDoChoNgoiId); // RB-18, RB-37
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

                // Ghế chưa chọn chỗ được xếp chỗ trống ngay (RB-36), để số chỗ đã giữ luôn khớp chỗ trên xe
                if (lich.SoDoChoNgoiId.HasValue && XepChoNgoi.XepChoConThieu(_context, lich.Id) > 0)
                {
                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (DbUpdateException)
                    {
                        giaoDichCsdl.Rollback();
                        TempData["Loi"] = "Có người vừa chọn chỗ trên xe cùng lúc với bạn. Bạn bấm giữ chỗ lại nhé.";
                        return RedirectToAction("ChonCho");
                    }
                }

                giaoDichCsdl.Commit();
            }

            HttpContext.Session.Remove(KhoaNhap);
            TempData["ThanhCong"] = $"Đã giữ {soGhe} chỗ đến {hanGiuCho:HH:mm dd/MM}. Gửi link mời cho các thành viên để họ nhận ghế và trả phần mình.";
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

            XuLyNeuQuaHan(id);
            var datTour = _context.DatTours.AsNoTracking().Include(x => x.LichKhoiHanhTour)
                .FirstOrDefault(x => x.Id == id && x.LoaiDon == "Nhom");
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
                LyDoKhongNhan = LyDoDonKhongNhan(datTour),
                LinkMoi =datTour.MaMoi != null && !don.DaHuy
                    ? Url.Action("Moi", "DonNhom", new { ma = datTour.MaMoi }, Request.Scheme)
                    : null,
                DsGhe = dsGhe.Select((h, i) => new GheNhomHienThi
                {
                    Id = h.Id,
                    ThuTu = i + 1,
                    LoaiHanhKhach = h.LoaiHanhKhach,
                    GiaLucDat = h.GiaLucDat,
                    TienGiam = h.TienGiam,
                    MaThanhToan = h.MaThanhToan,
                    // Chỉ hiện tên, không hiện giấy tờ của người khác (RB-20)
                    TenChu = !string.IsNullOrWhiteSpace(h.HoTen) ? h.HoTen : h.TaiKhoan?.HoTen,
                    LaCuaToi = h.TaiKhoanId == taiKhoanId.Value,
                    DaCoChu = h.TaiKhoanId.HasValue || !string.IsNullOrWhiteSpace(h.HoTen),
                    CoTaiKhoan = h.TaiKhoanId.HasValue,
                    MaCho = choTheoGhe.TryGetValue(h.Id, out var ma) ? ma : null,
                    TrangThai = TrangThaiGhe.Tinh(h, dsGiaoDich),
                    BiTuChoi = TrangThaiGhe.LucBiTuChoi(h.Id, dsGiaoDich).HasValue
                }).ToList()
            };
            model.SoDo = TaoSoDoNhom(datTour, model.DsGhe);
            return View(model);
        }

        // Dữ liệu "Xe của nhóm". Null khi đơn đã hủy, lịch không có sơ đồ, hoặc nhóm chưa chọn chỗ nào.
        private SoDoNhom? TaoSoDoNhom(DatTours datTour, List<GheNhomHienThi> dsGhe)
        {
            var soDoId = datTour.LichKhoiHanhTour?.SoDoChoNgoiId;
            if (datTour.TrangThai == "DaHuy" || soDoId == null || !dsGhe.Any(g => g.MaCho != null))
            {
                return null;
            }

            var soDo = _context.SoDoChoNgois.AsNoTracking().Include(s => s.PhuongTien).FirstOrDefault(s => s.Id == soDoId);
            if (soDo == null)
            {
                return null;
            }

            var gheTheoId = dsGhe.ToDictionary(g => g.Id);
            var nguoiTheoCho = _context.ChoNgoiChuyenDis.AsNoTracking()
                .Where(c => c.LichKhoiHanhTourId == datTour.LichKhoiHanhTourId)
                .ToDictionary(c => c.ViTriChoNgoiId, c => c.HanhKhachDatTourId);
            // Chỗ chuyến này không bán (RB-37): không hiện là "còn trống"
            var khongMoBan = SucChuaLich.ViTriKhoa(_context, datTour.LichKhoiHanhTourId);

            return new SoDoNhom
            {
                TenSoDo = soDo.TenSoDo,
                SoHang = soDo.SoHang,
                SoCot = soDo.SoCot,
                LaMayBay = soDo.PhuongTien?.TenPhuongTien == "Máy bay",
                DsCho = _context.ViTriChoNgois.AsNoTracking().Where(v => v.SoDoChoNgoiId == soDo.Id)
                    .OrderBy(v => v.Hang).ThenBy(v => v.Cot).ToList()
                    .Select(v =>
                    {
                        var cho = new ChoTrenSoDo { Ma = v.MaViTri, Hang = v.Hang, Cot = v.Cot };
                        if (!nguoiTheoCho.TryGetValue(v.Id, out var gheId))
                        {
                            if (khongMoBan.Contains(v.Id))
                            {
                                cho.Loai = "khong-ban";
                            }
                            return cho;
                        }
                        if (!gheTheoId.TryGetValue(gheId, out var ghe))
                        {
                            cho.Loai = "khach-khac";
                            return cho;
                        }
                        cho.Loai = ghe.TrangThai switch
                        {
                            TrangThaiGhe.DaTra => "da-tra",
                            TrangThaiGhe.ChoXacNhan => "cho",
                            TrangThaiGhe.DaDien => "da-dien",
                            TrangThaiGhe.DaNhan => "da-nhan",
                            _ => "trong"
                        };
                        cho.Ten = ghe.TenChu ?? "Ghế trống";
                        cho.TrangThai = ghe.TrangThai;
                        return cho;
                    }).ToList()
            };
        }

        // ---------- Trưởng nhóm quản lý đơn: đổi link, mời ra, bớt ghế, chốt, hủy (N2) ----------

        // Đơn nhóm của người đang đăng nhập (RB-21: chỉ trưởng nhóm), nạp kèm lịch và ghế
        private DatTours? LayDonCuaTruongNhom(int donId)
        {
            var taiKhoanId = TaiKhoanDangNhap();
            if (!taiKhoanId.HasValue)
            {
                return null;
            }
            return _context.DatTours
                .Include(x => x.LichKhoiHanhTour)
                .Include(x => x.HanhKhachDatTours)
                .FirstOrDefault(x => x.Id == donId && x.LoaiDon == "Nhom" && x.TaiKhoanId == taiKhoanId.Value);
        }

        // Trạng thái của mọi ghế còn hiệu lực trong đơn, tính từ giao dịch
        private List<string> TrangThaiCacGhe(DatTours don)
        {
            var dsGhe = don.HanhKhachDatTours.Where(h => h.NgayNha == null).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            var dsGiaoDich = _context.GiaoDichThanhToans.AsNoTracking()
                .Where(g => g.HanhKhachDatTourId != null && dsGheId.Contains(g.HanhKhachDatTourId.Value)).ToList();
            return dsGhe.Select(h => TrangThaiGhe.Tinh(h, dsGiaoDich)).ToList();
        }

        // Thu hồi link cũ: đổi mã mời, người đã nhận ghế vẫn giữ ghế (RB-23)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiLinkMoi(int id)
        {
            var don = LayDonCuaTruongNhom(id);
            if (don == null)
            {
                return NotFound();
            }
            if (!DonConNhanGhe(don))
            {
                TempData["Loi"] = LyDoDonKhongNhan(don);
                return RedirectToAction("ChiTiet", new { id });
            }

            don.MaMoi = XuLyDon.TaoMaMoi(_context);
            _context.SaveChanges();
            TempData["ThanhCong"] = "Đã tạo link mời mới. Link cũ không dùng được nữa, bạn gửi lại lời mời mới cho nhóm.";
            return RedirectToAction("ChiTiet", new { id });
        }

        // Mời thành viên ra khỏi ghế họ đã nhận mà chưa trả: ghế về trống, giữ chỗ ngồi để người khác nhận
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoiRaKhoiGhe(int id)
        {
            var ghe = _context.HanhKhachDatTours.AsNoTracking().FirstOrDefault(h => h.Id == id);
            var don = ghe == null ? null : LayDonCuaTruongNhom(ghe.DatTourId);
            if (ghe == null || don == null)
            {
                return NotFound();
            }
            if (!DonConNhanGhe(don))
            {
                TempData["Loi"] = LyDoDonKhongNhan(don);
                return RedirectToAction("ChiTiet", new { id = don.Id });
            }
            if (!ghe.TaiKhoanId.HasValue || ghe.TaiKhoanId == don.TaiKhoanId)
            {
                TempData["Loi"] = "Ghế này không có thành viên nào nhận. Ghế của bạn thì dùng Trả lại ghế.";
                return RedirectToAction("ChiTiet", new { id = don.Id });
            }

            // Có điều kiện: đúng người đang giữ ghế, ghế chưa nhả, chưa báo chuyển khoản hay đã trả
            int soDong = _context.Database.ExecuteSqlInterpolated($@"
                UPDATE HanhKhachDatTours SET TaiKhoanId = NULL, HoTen = NULL, LoaiGiayTo = NULL, SoGiayTo = NULL
                WHERE Id = {id} AND NgayNha IS NULL AND TaiKhoanId = {ghe.TaiKhoanId.Value}
                  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans WHERE HanhKhachDatTourId = {id} AND TrangThai IN ('ChoXacNhan', 'ThanhCong'))");

            TempData[soDong == 0 ? "Loi" : "ThanhCong"] = soDong == 0
                ? "Thành viên vừa báo chuyển khoản hoặc đã trả lại ghế, ghế không bị đổi."
                : "Ghế đã trở về trống, người khác có thể nhận qua link mời.";
            return RedirectToAction("ChiTiet", new { id = don.Id });
        }

        // Bớt một ghế chưa trả: nhả ghế, bỏ chỗ ngồi, trả 1 chỗ cho lịch. Đơn phải còn ít nhất 1 ghế.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BotGhe(int id)
        {
            var ghe = _context.HanhKhachDatTours.AsNoTracking().FirstOrDefault(h => h.Id == id);
            var don = ghe == null ? null : LayDonCuaTruongNhom(ghe.DatTourId);
            if (ghe == null || don == null)
            {
                return NotFound();
            }
            if (!DonConNhanGhe(don))
            {
                TempData["Loi"] = LyDoDonKhongNhan(don);
                return RedirectToAction("ChiTiet", new { id = don.Id });
            }

            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                int soDong = _context.Database.ExecuteSqlInterpolated($@"
                    UPDATE HanhKhachDatTours SET NgayNha = {GioVN.BayGio}
                    WHERE Id = {id} AND DatTourId = {don.Id} AND NgayNha IS NULL
                      AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans WHERE HanhKhachDatTourId = {id} AND TrangThai IN ('ChoXacNhan', 'ThanhCong'))
                      AND (SELECT COUNT(*) FROM HanhKhachDatTours WHERE DatTourId = {don.Id} AND NgayNha IS NULL) > 1");
                if (soDong == 0)
                {
                    giaoDichCsdl.Rollback();
                    TempData["Loi"] = "Không bớt được ghế này: ghế đã báo chuyển khoản, đã trả, hoặc đơn chỉ còn một ghế.";
                    return RedirectToAction("ChiTiet", new { id = don.Id });
                }
                _context.Database.ExecuteSqlInterpolated($"DELETE FROM ChoNgoiChuyenDis WHERE HanhKhachDatTourId = {id}");
                _context.Database.ExecuteSqlInterpolated(
                    $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon + 1 WHERE Id = {don.LichKhoiHanhTourId} AND TrangThai <> {TrangThaiLich.DaHuy}");
                giaoDichCsdl.Commit();
            }

            TempData["ThanhCong"] = "Đã bớt một ghế, chỗ đã trả lại cho lịch.";
            return RedirectToAction("ChiTiet", new { id = don.Id });
        }

        // Chốt sớm: chỉ khi không còn ghế chờ admin xác nhận và có ít nhất một ghế đã trả (quyết định N2)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChotDon(int id)
        {
            var don = LayDonCuaTruongNhom(id);
            if (don == null)
            {
                return NotFound();
            }
            if (don.TrangThai != "GiuCho")
            {
                TempData["Loi"] = "Đơn đã chốt hoặc đã hủy.";
                return RedirectToAction("ChiTiet", new { id });
            }

            var dsTrangThai = TrangThaiCacGhe(don);
            int soCho = dsTrangThai.Count(t => t == TrangThaiGhe.ChoXacNhan);
            if (soCho > 0)
            {
                TempData["Loi"] = $"Còn {soCho} ghế đang chờ TravelVN xác nhận tiền. Bạn chốt sau khi xác nhận xong nhé.";
                return RedirectToAction("ChiTiet", new { id });
            }
            if (!dsTrangThai.Contains(TrangThaiGhe.DaTra))
            {
                TempData["Loi"] = "Chưa ghế nào trả tiền nên chưa chốt được. Nếu cả nhóm không đi nữa, bạn hủy đơn.";
                return RedirectToAction("ChiTiet", new { id });
            }

            int soNha = XuLyDon.ChotDonNhom(_context, don.Id, don.LichKhoiHanhTourId);
            TempData[soNha < 0 ? "Loi" : "ThanhCong"] = soNha < 0
                ? "Đơn vừa được chốt hoặc hủy trước đó."
                : soNha == 0
                    ? "Đã chốt đơn, mọi ghế đều đã trả."
                    : $"Đã chốt đơn. {soNha} ghế chưa trả đã được nhả và trả chỗ cho lịch.";
            return RedirectToAction("ChiTiet", new { id });
        }

        // Hủy đơn nhóm: chỉ khi chưa ghế nào trả hoặc báo chuyển khoản; đã có tiền thì liên hệ TravelVN (quyết định N2)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult HuyDonNhom(int id)
        {
            var don = LayDonCuaTruongNhom(id);
            if (don == null)
            {
                return NotFound();
            }
            if (don.TrangThai == "DaHuy")
            {
                TempData["Loi"] = "Đơn này đã hủy trước đó.";
                return RedirectToAction("ChiTiet", new { id });
            }
            if (TrangThaiCacGhe(don).Any(t => t == TrangThaiGhe.DaTra || t == TrangThaiGhe.ChoXacNhan))
            {
                TempData["Loi"] = "Đã có ghế trả tiền hoặc báo chuyển khoản. Để hủy và hoàn tiền, bạn liên hệ TravelVN.";
                return RedirectToAction("ChiTiet", new { id });
            }

            XuLyDon.HuyDon(_context, don);
            TempData["ThanhCong"] = "Đã hủy đơn nhóm, các chỗ đã trả lại cho lịch.";
            return RedirectToAction("ChiTiet", new { id });
        }

        // ---------- Link mời: xem không cần đăng nhập, nhận ghế phải đăng nhập (N3) ----------

        // Lý do đơn không còn nhận thay đổi (nhận ghế, sửa, trả tiền, bớt ghế, đổi link); null là còn nhận.
        // Giao diện và máy chủ cùng dùng hàm này để nút hiện ra là bấm được, câu báo nói đúng lý do.
        private static string? LyDoDonKhongNhan(DatTours don)
        {
            if (don.LoaiDon != "Nhom" || don.LichKhoiHanhTour == null) return "Không tìm thấy đơn nhóm.";
            if (don.TrangThai == "DaHuy") return "Đơn nhóm đã hủy.";
            if (don.TrangThai == "DaXacNhan") return "Đơn đã chốt, danh sách đi đã cố định.";
            if (don.LichKhoiHanhTour.TrangThai == TrangThaiLich.DaHuy) return "Lịch khởi hành đã bị hủy. TravelVN sẽ liên hệ trưởng nhóm.";
            if (don.LichKhoiHanhTour.NgayKhoiHanh.Date <= GioVN.HomNay) return "Đã tới ngày khởi hành nên đơn không nhận thay đổi nữa. Cần hỗ trợ thì liên hệ TravelVN.";
            // Trang đã chạy XuLyNeuQuaHan trước, nên quá hạn mà vẫn GiuCho nghĩa là đang đợi admin xác nhận ghế đã báo chuyển khoản
            if (!(don.HanGiuCho > GioVN.BayGio)) return "Đã hết hạn giữ chỗ, ghế chưa trả đã được nhả. Đơn sẽ tự chốt khi TravelVN xác nhận xong các ghế đã báo chuyển khoản.";
            return null;
        }

        private static bool DonConNhanGhe(DatTours don) => LyDoDonKhongNhan(don) == null;

        // Lý do một ghế không sửa được: ghế đã có tiền, hoặc đơn không còn nhận thay đổi
        private static string? LyDoGheKhongSua(DatTours don, string trangThai) =>
            trangThai is TrangThaiGhe.DaTra or TrangThaiGhe.ChoXacNhan
                ? "Ghế đã báo chuyển khoản hoặc đã trả nên thông tin được khóa. Cần sửa thì liên hệ TravelVN."
                : LyDoDonKhongNhan(don);

        private DatTours? LayDonTheoMaMoi(string? ma) =>
            string.IsNullOrEmpty(ma)
                ? null
                : _context.DatTours.AsNoTracking()
                    .Include(x => x.LichKhoiHanhTour).ThenInclude(l => l!.Tour)
                    .FirstOrDefault(x => x.MaMoi == ma && x.LoaiDon == "Nhom");

        public IActionResult Moi(string? ma)
        {
            var don = LayDonTheoMaMoi(ma);
            if (don != null && don.TrangThai == "GiuCho" && don.HanGiuCho <= GioVN.BayGio)
            {
                // Quá hạn: xử lý (có thể được gia hạn) rồi đọc lại
                XuLyNeuQuaHan(don.Id);
                don = LayDonTheoMaMoi(ma);
            }
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
                        TienGiam = x.h.TienGiam,
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
        private static bool GheDuocSua(DatTours don, string trangThai) => LyDoGheKhongSua(don, trangThai) == null;

        // Chỗ ngồi đổi được tới trước ngày đi, khi đơn chưa hủy và lịch có sơ đồ
        private static bool GheDuocDoiCho(DatTours don) =>
            don.TrangThai != "DaHuy"
            && don.LichKhoiHanhTour?.SoDoChoNgoiId != null
            && don.LichKhoiHanhTour.TrangThai != TrangThaiLich.DaHuy
            && don.LichKhoiHanhTour.NgayKhoiHanh.Date > GioVN.HomNay;

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
                TienGiam = ghe.TienGiam,
                LoaiHanhKhach = ghe.LoaiHanhKhach,
                MaThanhToan = ghe.MaThanhToan,
                DuocTra = duocTra,
                LyDoKhongTra = LyDoDonKhongNhan(don),
                LucBiTuChoi = TrangThaiGhe.LucBiTuChoi(ghe.Id,
                    _context.GiaoDichThanhToans.AsNoTracking().Where(g => g.HanhKhachDatTourId == ghe.Id).ToList()),
                TraHo = traHo,
                ChuyenKhoan = duocTra && ghe.MaThanhToan != null
                    ? ThongTinChuyenKhoan.Tao(_cauHinh, ghe.SoTienPhaiTra, ghe.MaThanhToan)
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
                DonDaChot = don.TrangThai == "DaXacNhan",
                DaGiaHan = don.DaGiaHan,
                LoaiHanhKhach = ghe.LoaiHanhKhach,
                GiaLucDat = ghe.GiaLucDat,
                TienGiam = ghe.TienGiam,
                MaCho = maCho,
                TrangThai = trangThai,
                DienHo = ghe.TaiKhoanId != taiKhoanId,
                LaTruongNhom = laTruongNhom,
                DuocSua = GheDuocSua(don, trangThai),
                LyDoKhongSua = LyDoGheKhongSua(don, trangThai),
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

            XuLyNeuQuaHan(DonCuaGhe(id));
            var ghe = LayGheDuocMo(id, taiKhoanId.Value, out var laTruongNhom);
            if (ghe == null)
            {
                // Link cũ tới ghế đã bị nhả, đã trả lại hoặc bị trưởng nhóm mời ra: giải thích thay vì báo lỗi 404
                TempData["Loi"] = "Ghế này không còn là của bạn: ghế đã được trả lại, trưởng nhóm đã bớt ghế hoặc mời bạn ra, hoặc đơn đã chốt, đã hủy.";
                return RedirectToAction("LichSu", "DatTour");
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

            if (LyDoGheKhongSua(ghe.DatTour!, TinhTrangThaiGhe(ghe)) is string lyDoSua)
            {
                TempData["Loi"] = lyDoSua;
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
            if (LyDoGheKhongSua(don, TinhTrangThaiGhe(ghe)) is string lyDoTraLai)
            {
                TempData["Loi"] = lyDoTraLai;
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

            XuLyNeuQuaHan(DonCuaGhe(id));
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

            var trangThaiTra = TinhTrangThaiGhe(ghe);
            if (!GheDuocTra(ghe.DatTour!, trangThaiTra))
            {
                TempData["Loi"] = LyDoDonKhongNhan(ghe.DatTour!)
                    ?? (trangThaiTra is TrangThaiGhe.DaTra or TrangThaiGhe.ChoXacNhan
                        ? "Ghế này đã báo chuyển khoản rồi."
                        : "Ghế chưa điền đủ họ tên và giấy tờ, điền xong mới báo chuyển khoản được.");
                return QuayLai();
            }

            int soDong = _context.Database.ExecuteSqlInterpolated($@"
                INSERT INTO GiaoDichThanhToans (HanhKhachDatTourId, NguoiTraId, SoTien, PhuongThuc, TrangThai, NgayTao)
                SELECT h.Id, {taiKhoanId.Value}, h.GiaLucDat - h.TienGiam, {phuongThuc}, 'ChoXacNhan', {GioVN.BayGio}
                FROM HanhKhachDatTours h
                WHERE h.Id = {id} AND h.NgayNha IS NULL
                  AND EXISTS (SELECT 1 FROM DatTours d WHERE d.Id = h.DatTourId AND d.TrangThai = 'GiuCho')
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
            var khongMoBan = SucChuaLich.ViTriKhoa(_context, lich.Id);
            foreach (var v in model.DsViTri)
            {
                v.DaCoNguoi = cuaNguoiKhac.Contains(v.Id);
                v.KhongMoBan = khongMoBan.Contains(v.Id);
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
            // Ô để trống: giữ chỗ hiện tại. RB-18, RB-37: chỗ phải thuộc sơ đồ của lịch và đang mở bán.
            if (chon.HasValue && XepChoNgoi.ViTriChonDuoc(_context, don.LichKhoiHanhTourId, don.LichKhoiHanhTour!.SoDoChoNgoiId).Contains(chon.Value))
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
