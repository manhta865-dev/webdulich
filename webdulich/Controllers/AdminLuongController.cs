using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Lương nhân viên (RB-46): bảng lương tháng, chốt, đánh dấu đã trả, thưởng/phạt, mức lương từng người.
    public class AdminLuongController : Controller
    {
        private readonly DuLichDbContext _context;
        private readonly IConfiguration _cauHinh;

        public AdminLuongController(DuLichDbContext context, IConfiguration cauHinh)
        {
            _context = context;
            _cauHinh = cauHinh;
        }

        private bool LaAdmin() => HttpContext.Session.GetString("role") == "admin";
        private int? AdminId() => HttpContext.Session.GetInt32("taiKhoanId");

        private static (int Nam, int Thang) ChuanThang(int? nam, int? thang)
        {
            var homNay = GioVN.HomNay;
            int n = nam is >= 2000 and <= 2100 ? nam.Value : homNay.Year;
            int t = thang is >= 1 and <= 12 ? thang.Value : homNay.Month;
            return (n, t);
        }

        // Tháng sau tháng hiện tại thì chưa chốt được (chưa có số liệu thật)
        private static bool LaThangTuongLai(int nam, int thang)
        {
            var homNay = GioVN.HomNay;
            return nam > homNay.Year || (nam == homNay.Year && thang > homNay.Month);
        }

        private PhieuLuong TinhPhieu(NhanVien nv, int nam, int thang) =>
            TinhLuong.Tinh(_context, nv, nam, thang, TinhLuong.SoNgayPhepNam(_cauHinh), TinhLuong.SoNgayCongChuan(_cauHinh));

        public IActionResult Index(int? nam, int? thang)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            (int n, int t) = ChuanThang(nam, thang);

            // Nhân viên đang làm việc, cộng người đã nghỉ hẳn nhưng có bảng lương tháng này
            var dsNv = _context.NhanViens.AsNoTracking().Include(x => x.TaiKhoan)
                .Where(x => x.DangLamViec || _context.BangLuongs.Any(b => b.NhanVienId == x.Id && b.Nam == n && b.Thang == t))
                .OrderBy(x => x.TaiKhoan!.VaiTro).ThenBy(x => x.TaiKhoan!.HoTen)
                .ToList();

            ViewBag.Nam = n;
            ViewBag.Thang = t;
            ViewBag.TuongLai = LaThangTuongLai(n, t);
            ViewBag.SoNgayPhepNam = TinhLuong.SoNgayPhepNam(_cauHinh);
            ViewBag.SoNgayCongChuan = TinhLuong.SoNgayCongChuan(_cauHinh);
            return View(dsNv.Select(nv => TinhPhieu(nv, n, t)).ToList());
        }

        public IActionResult ChiTiet(int id, int? nam, int? thang)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            var nv = _context.NhanViens.AsNoTracking().Include(x => x.TaiKhoan).FirstOrDefault(x => x.Id == id);
            if (nv == null) return NotFound();
            (int n, int t) = ChuanThang(nam, thang);
            ViewBag.TuongLai = LaThangTuongLai(n, t);
            return View(TinhPhieu(nv, n, t));
        }

        // Chốt một người (id) hoặc cả tháng (id rỗng): chụp lại số liệu đang tạm tính vào BangLuongs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Chot(int? id, int nam, int thang, string? quayVe)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            var adminId = AdminId();
            if (!adminId.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");
            if (LaThangTuongLai(nam, thang))
            {
                TempData["error"] = "Chưa chốt được lương của tháng chưa tới.";
                return VeTrang(quayVe, id, nam, thang);
            }

            var dsNv = _context.NhanViens.AsNoTracking().Include(x => x.TaiKhoan)
                .Where(x => id.HasValue ? x.Id == id.Value : x.DangLamViec)
                .Where(x => !_context.BangLuongs.Any(b => b.NhanVienId == x.Id && b.Nam == nam && b.Thang == thang))
                .ToList();

            int soChot = 0;
            var chuaCoMuc = new List<string>();
            foreach (var nv in dsNv)
            {
                var p = TinhPhieu(nv, nam, thang);
                if (!p.CoMucLuong)
                {
                    chuaCoMuc.Add(nv.TaiKhoan?.HoTen ?? "");
                    continue;
                }
                _context.BangLuongs.Add(new BangLuong
                {
                    NhanVienId = nv.Id,
                    Nam = nam,
                    Thang = thang,
                    LuongCoBan = p.LuongCoBan,
                    TienCongNgay = p.TienCongNgay,
                    SoNgayDiTour = p.SoNgayDiTour,
                    SoNgayNghiTruLuong = p.SoNgayNghiTruLuong,
                    SoNgayCongChuan = p.SoNgayCongChuan,
                    NguoiChotId = adminId.Value,
                    NgayChot = GioVN.BayGio
                });
                soChot++;
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Hai admin chốt cùng lúc: UNIQUE (NhanVienId, Nam, Thang) chặn bản thứ hai
                TempData["error"] = "Có bảng lương vừa được chốt ở nơi khác. Mở lại trang để xem số mới nhất.";
                return VeTrang(quayVe, id, nam, thang);
            }

            var loi = chuaCoMuc.Any() ? $" Chưa chốt {chuaCoMuc.Count} người vì chưa đặt mức lương: {string.Join(", ", chuaCoMuc)}." : "";
            if (soChot > 0) TempData["success"] = $"Đã chốt lương tháng {thang}/{nam} cho {soChot} người.{loi}";
            else TempData["error"] = chuaCoMuc.Any() ? loi.Trim() : "Không có bảng lương nào cần chốt.";
            return VeTrang(quayVe, id, nam, thang);
        }

        // Mở lại bảng lương đã chốt nhưng chưa trả (để sửa thưởng/phạt hoặc tính lại). Đã trả thì không mở được.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoLai(int id, int nam, int thang)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            int soDong = _context.Database.ExecuteSqlInterpolated(
                $"DELETE FROM BangLuongs WHERE NhanVienId = {id} AND Nam = {nam} AND Thang = {thang} AND NgayTra IS NULL");
            TempData[soDong == 1 ? "success" : "error"] = soDong == 1
                ? "Đã mở lại bảng lương, số liệu quay về tạm tính."
                : "Không mở lại được: bảng lương đã trả hoặc chưa chốt.";
            return RedirectToAction("ChiTiet", new { id, nam, thang });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DaTra(int id, int nam, int thang, string? quayVe)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            var adminId = AdminId();
            if (!adminId.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");
            int soDong = _context.Database.ExecuteSqlInterpolated($@"
                UPDATE BangLuongs SET NguoiTraId = {adminId.Value}, NgayTra = {GioVN.BayGio}
                WHERE NhanVienId = {id} AND Nam = {nam} AND Thang = {thang} AND NgayTra IS NULL");
            TempData[soDong == 1 ? "success" : "error"] = soDong == 1
                ? "Đã đánh dấu đã trả lương."
                : "Chưa đánh dấu được: bảng lương chưa chốt hoặc đã trả trước đó.";
            return VeTrang(quayVe, id, nam, thang);
        }

        // Thưởng (+) hoặc phạt (−) cho tháng chưa chốt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ThemKhoan(int id, int nam, int thang, decimal? soTien, bool laPhat, string? noiDung)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            var adminId = AdminId();
            if (!adminId.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            noiDung = noiDung?.Trim();
            string? loi = !soTien.HasValue || soTien <= 0 || soTien > 1_000_000_000 ? "Nhập số tiền lớn hơn 0."
                : string.IsNullOrEmpty(noiDung) ? "Ghi nội dung khoản thưởng hoặc phạt."
                : noiDung.Length > 300 ? "Nội dung tối đa 300 ký tự."
                : _context.BangLuongs.Any(b => b.NhanVienId == id && b.Nam == nam && b.Thang == thang) ? "Bảng lương tháng này đã chốt. Mở lại nếu cần sửa."
                : !_context.NhanViens.Any(x => x.Id == id) ? "Không tìm thấy nhân viên."
                : null;
            if (loi != null)
            {
                TempData["error"] = loi;
                return RedirectToAction("ChiTiet", new { id, nam, thang });
            }

            _context.KhoanLuongs.Add(new KhoanLuong
            {
                NhanVienId = id,
                Nam = nam,
                Thang = thang,
                SoTien = laPhat ? -soTien!.Value : soTien!.Value,
                NoiDung = noiDung!,
                NguoiTaoId = adminId.Value,
                NgayTao = GioVN.BayGio
            });
            _context.SaveChanges();
            TempData["success"] = laPhat ? "Đã thêm khoản phạt." : "Đã thêm khoản thưởng.";
            return RedirectToAction("ChiTiet", new { id, nam, thang });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaKhoan(int khoanId)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            var k = _context.KhoanLuongs.Find(khoanId);
            if (k == null) return NotFound();
            if (_context.BangLuongs.Any(b => b.NhanVienId == k.NhanVienId && b.Nam == k.Nam && b.Thang == k.Thang))
            {
                TempData["error"] = "Bảng lương tháng này đã chốt. Mở lại nếu cần sửa.";
            }
            else
            {
                _context.KhoanLuongs.Remove(k);
                _context.SaveChanges();
                TempData["success"] = "Đã xóa khoản.";
            }
            return RedirectToAction("ChiTiet", new { id = k.NhanVienId, nam = k.Nam, thang = k.Thang });
        }

        public IActionResult MucLuong(int id)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            var nv = _context.NhanViens.AsNoTracking().Include(x => x.TaiKhoan).FirstOrDefault(x => x.Id == id);
            if (nv == null) return NotFound();
            NapMucLuong(nv);
            return View(new MucLuongForm { NhanVienId = id, ApDungTu = new DateTime(GioVN.HomNay.Year, GioVN.HomNay.Month, 1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MucLuong(MucLuongForm form)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            var nv = _context.NhanViens.AsNoTracking().Include(x => x.TaiKhoan).FirstOrDefault(x => x.Id == form.NhanVienId);
            if (nv == null) return NotFound();

            if (form.ApDungTu.HasValue)
            {
                var ngay = form.ApDungTu.Value.Date;
                if (_context.MucLuongNhanViens.Any(m => m.NhanVienId == nv.Id && m.ApDungTu == ngay))
                    ModelState.AddModelError(nameof(form.ApDungTu), "Đã có mức lương áp dụng từ ngày này. Chọn ngày khác.");
                // Không đổi lương của tháng đã chốt: ngày áp dụng phải sau tháng chốt gần nhất
                var chotCuoi = _context.BangLuongs.Where(b => b.NhanVienId == nv.Id)
                    .OrderByDescending(b => b.Nam).ThenByDescending(b => b.Thang).Select(b => new { b.Nam, b.Thang }).FirstOrDefault();
                if (chotCuoi != null && ngay < new DateTime(chotCuoi.Nam, chotCuoi.Thang, 1).AddMonths(1))
                    ModelState.AddModelError(nameof(form.ApDungTu), $"Lương đã chốt đến tháng {chotCuoi.Thang}/{chotCuoi.Nam}. Ngày áp dụng phải từ tháng sau đó.");
            }
            if (!ModelState.IsValid)
            {
                NapMucLuong(nv);
                return View(form);
            }

            _context.MucLuongNhanViens.Add(new MucLuongNhanVien
            {
                NhanVienId = nv.Id,
                LuongCoBan = form.LuongCoBan!.Value,
                TienCongNgay = form.TienCongNgay!.Value,
                ApDungTu = form.ApDungTu!.Value.Date,
                GhiChu = string.IsNullOrWhiteSpace(form.GhiChu) ? null : form.GhiChu.Trim(),
                NgayTao = GioVN.BayGio
            });
            _context.SaveChanges();
            TempData["success"] = $"Đã đặt mức lương mới cho {nv.TaiKhoan?.HoTen}, áp dụng từ {form.ApDungTu.Value:dd/MM/yyyy}.";
            return RedirectToAction("MucLuong", new { id = nv.Id });
        }

        private void NapMucLuong(NhanVien nv)
        {
            ViewBag.NhanVien = nv;
            ViewBag.DsMuc = _context.MucLuongNhanViens.AsNoTracking()
                .Where(m => m.NhanVienId == nv.Id).OrderByDescending(m => m.ApDungTu).ToList();
        }

        private IActionResult VeTrang(string? quayVe, int? id, int nam, int thang) =>
            quayVe == "chitiet" && id.HasValue
                ? RedirectToAction("ChiTiet", new { id, nam, thang })
                : RedirectToAction("Index", new { nam, thang });
    }
}
