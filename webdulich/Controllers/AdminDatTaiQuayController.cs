using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Đặt tại quầy (RB-42): khách đến văn phòng, admin tạo đơn hộ. Đơn là đơn cá nhân đã xác nhận, có giao dịch
    // "Tiền mặt tại quầy" do admin xác nhận, trừ chỗ và khóa chỗ ngồi trong cùng một giao dịch CSDL như đặt online,
    // nên chỗ của khách tại quầy bị khóa trên sơ đồ, có trong danh sách khách của hướng dẫn viên và trong doanh thu.
    public class AdminDatTaiQuayController : Controller
    {
        private readonly DuLichDbContext _context;

        public AdminDatTaiQuayController(DuLichDbContext context)
        {
            _context = context;
        }

        private bool LaAdmin() => HttpContext.Session.GetString("role") == "admin";

        // Lịch còn đặt tại quầy được: chưa hủy và chưa tới ngày đi. Lịch tạm ngưng bán online vẫn bán tại quầy được.
        private LichKhoiHanhTour? LayLich(int lichId) =>
            _context.LichKhoiHanhTours
                .Include(l => l.Tour).ThenInclude(t => t!.PhuongTienInfo)
                .Include(l => l.SoDoChoNgoi)
                .FirstOrDefault(l => l.Id == lichId);

        private static string? LyDoKhongDatDuoc(LichKhoiHanhTour lich) =>
            lich.TrangThai == TrangThaiLich.DaHuy ? "Lịch này đã hủy."
            : lich.NgayKhoiHanh.Date <= GioVN.HomNay ? "Lịch đã tới hoặc qua ngày khởi hành."
            : null;

        // Loại hành khách từng ghế: người lớn trước, trẻ nhỏ sau
        private static List<string> DanhSachLoai(int soLon, int soTre) =>
            Enumerable.Repeat("Người lớn", soLon).Concat(Enumerable.Repeat("Trẻ nhỏ", soTre)).ToList();

        // Điền phần hiển thị: giá, số chỗ, sơ đồ xe với chỗ đã có người và chỗ không mở bán
        private void DienThongTin(DatTaiQuayViewModel m, LichKhoiHanhTour lich, List<int?>? viTriDaChon = null)
        {
            m.LichId = lich.Id;
            m.TourId = lich.TourId;
            m.TenTour = lich.Tour?.TenTour ?? string.Empty;
            m.NgayDi = lich.NgayKhoiHanh;
            m.GioKhoiHanh = lich.GioKhoiHanh;
            m.SoChoCon = lich.SoChoCon;
            m.GiaNguoiLon = lich.GiaNguoiLonHienThi;
            m.GiaTreEm = lich.GiaTreEmHienThi;
            var dsLoai = DanhSachLoai(m.SoNguoiLon, m.SoTreNho);
            while (m.HoTen.Count < dsLoai.Count) m.HoTen.Add(null);

            if (lich.SoDoChoNgoi == null)
            {
                return;
            }
            var daCoNguoi = _context.ChoNgoiChuyenDis.Where(c => c.LichKhoiHanhTourId == lich.Id).Select(c => c.ViTriChoNgoiId).ToHashSet();
            var khongMoBan = SucChuaLich.ViTriKhoa(_context, lich.Id);
            m.SoDo = new ChonChoNgoiViewModel
            {
                TenTour = m.TenTour,
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
                DsHanhKhach = dsLoai.Select((loai, i) => new HanhKhachChonCho
                {
                    ThuTu = i,
                    HoTen = m.HoTen[i] ?? string.Empty,
                    LoaiHanhKhach = loai,
                    ViTriId = viTriDaChon != null && i < viTriDaChon.Count ? viTriDaChon[i] : null
                }).ToList(),
                ChoTuDongXep = true
            };
            foreach (var v in m.SoDo.DsViTri)
            {
                v.DaCoNguoi = daCoNguoi.Contains(v.Id);
                v.KhongMoBan = khongMoBan.Contains(v.Id);
            }
        }

        // Mở form; đổi số người thì tải lại form với số mới (sơ đồ có đúng số ô chọn chỗ)
        public IActionResult Index(int id, int soNguoiLon = 1, int soTreNho = 0)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            var lich = LayLich(id);
            if (lich == null)
            {
                return NotFound();
            }
            var lyDo = LyDoKhongDatDuoc(lich);
            if (lyDo != null)
            {
                TempData["error"] = lyDo + " Không đặt tại quầy được.";
                return RedirectToAction("LichKhoiHanh", "Admin", new { id = lich.TourId });
            }

            var m = new DatTaiQuayViewModel
            {
                SoNguoiLon = Math.Clamp(soNguoiLon, 1, 45),
                SoTreNho = Math.Clamp(soTreNho, 0, 45)
            };
            DienThongTin(m, lich);
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(DatTaiQuayViewModel m, List<int?>? viTri)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            var lich = LayLich(m.LichId);
            if (lich == null)
            {
                return NotFound();
            }
            viTri ??= new List<int?>();

            var lyDo = LyDoKhongDatDuoc(lich);
            if (lyDo != null) ModelState.AddModelError("", lyDo);
            if (m.SoNguoiLon < 1) ModelState.AddModelError(nameof(m.SoNguoiLon), "Cần ít nhất 1 người lớn.");
            if (m.SoTreNho < 0) ModelState.AddModelError(nameof(m.SoTreNho), "Số trẻ nhỏ không hợp lệ.");
            // Bỏ trống tên, số điện thoại do [Required] trong view model báo; ở đây chỉ kiểm định dạng số
            if (!string.IsNullOrWhiteSpace(m.DienThoai) && !DinhDanhTaiKhoan.LaSoDienThoai(m.DienThoai.Trim()))
                ModelState.AddModelError(nameof(m.DienThoai), DinhDanhTaiKhoan.LoiSoDienThoai);
            int soNguoi = m.SoNguoiLon + m.SoTreNho;
            if (soNguoi > lich.SoChoCon) ModelState.AddModelError("", $"Lịch chỉ còn {lich.SoChoCon} chỗ.");

            // Chỗ chọn tay: thuộc xe của lịch, đang mở bán (RB-18, RB-37), không trùng nhau trong đơn
            var hopLe = XepChoNgoi.ViTriChonDuoc(_context, lich.Id, lich.SoDoChoNgoiId);
            var daDung = new HashSet<int>();
            var dsCho = viTri.Take(soNguoi)
                .Select(v => v.HasValue && hopLe.Contains(v.Value) && daDung.Add(v.Value) ? v : null)
                .ToList();

            if (!ModelState.IsValid)
            {
                DienThongTin(m, lich, dsCho);
                return View(m);
            }

            var dsLoai = DanhSachLoai(m.SoNguoiLon, m.SoTreNho);
            var maChuyenDi = XuLyDon.TaoMaChuyenDi(_context);
            var bayGio = GioVN.BayGio;
            var don = new DatTours
            {
                LichKhoiHanhTourId = lich.Id,
                TaiKhoanId = null,
                MaChuyenDi = maChuyenDi,
                LoaiDon = "CaNhan",
                TrangThai = "DaXacNhan",
                NgayDat = bayGio,
                TenKhach = m.TenKhach.Trim(),
                DienThoai = m.DienThoai.Trim(),
                Email = m.Email?.Trim() ?? string.Empty,
                DiaChi = string.Empty,
                GhiChu = string.IsNullOrWhiteSpace(m.GhiChu) ? "Đặt tại quầy." : "Đặt tại quầy. " + m.GhiChu.Trim()
            };
            for (int i = 0; i < dsLoai.Count; i++)
            {
                var ten = i < m.HoTen.Count ? m.HoTen[i]?.Trim() : null;
                don.HanhKhachDatTours.Add(new HanhKhachDatTour
                {
                    LoaiHanhKhach = dsLoai[i],
                    HoTen = string.IsNullOrEmpty(ten) ? null : ten,
                    GiaLucDat = XuLyDon.GiaGhe(dsLoai[i], lich.GiaNguoiLonHienThi, lich.GiaTreEmHienThi)
                });
            }

            using (var giaoDichCsdl = _context.Database.BeginTransaction())
            {
                // Trừ chỗ có điều kiện (RB-12): hai người cùng lúc không làm vượt chỗ. Tạm ngưng bán online vẫn bán tại quầy.
                int soDong = _context.Database.ExecuteSqlInterpolated(
                    $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon - {soNguoi} WHERE Id = {lich.Id} AND SoChoCon >= {soNguoi} AND TrangThai <> {TrangThaiLich.DaHuy}");
                if (soDong == 0)
                {
                    giaoDichCsdl.Rollback();
                    TempData["error"] = "Lịch vừa hết chỗ cho số người này. Bạn kiểm tra lại số chỗ.";
                    return RedirectToAction("Index", new { id = lich.Id, soNguoiLon = m.SoNguoiLon, soTreNho = m.SoTreNho });
                }

                _context.DatTours.Add(don);
                _context.SaveChanges();

                // Chỗ chọn tay trước, ghế còn lại TravelVN xếp chỗ trống (RB-36)
                var dsGhe = don.HanhKhachDatTours.ToList();
                for (int i = 0; i < dsGhe.Count; i++)
                {
                    if (i < dsCho.Count && dsCho[i].HasValue)
                    {
                        _context.ChoNgoiChuyenDis.Add(new ChoNgoiChuyenDi { LichKhoiHanhTourId = lich.Id, ViTriChoNgoiId = dsCho[i]!.Value, HanhKhachDatTourId = dsGhe[i].Id });
                    }
                }
                try
                {
                    _context.SaveChanges();
                    if (XepChoNgoi.XepChoConThieu(_context, lich.Id) > 0)
                    {
                        _context.SaveChanges();
                    }
                }
                catch (DbUpdateException)
                {
                    giaoDichCsdl.Rollback();
                    TempData["error"] = "Có chỗ vừa được khách khác chọn. Bạn chọn lại chỗ nhé.";
                    return RedirectToAction("Index", new { id = lich.Id, soNguoiLon = m.SoNguoiLon, soTreNho = m.SoTreNho });
                }

                // Tiền mặt nhận tại quầy: giao dịch thành công, admin đang đăng nhập là người xác nhận
                decimal tongTien = dsGhe.Sum(g => g.GiaLucDat);
                if (tongTien > 0)
                {
                    _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
                    {
                        DatTourId = don.Id,
                        SoTien = tongTien,
                        PhuongThuc = "Tiền mặt tại quầy",
                        TrangThai = "ThanhCong",
                        NgayTao = bayGio,
                        NgayXacNhan = bayGio,
                        NguoiXacNhanId = HttpContext.Session.GetInt32("taiKhoanId")
                    });
                    _context.SaveChanges();
                }

                giaoDichCsdl.Commit();
            }

            TempData["success"] = $"Đã đặt tại quầy đơn {maChuyenDi} cho {soNguoi} khách, đã thu {don.HanhKhachDatTours.Sum(g => g.GiaLucDat):N0} VNĐ.";
            return RedirectToAction("SuaDonDatTour", "Admin", new { id = don.Id });
        }
    }
}
