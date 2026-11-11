using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Admin xem và duyệt đơn xin nghỉ của nhân viên (RB-45).
    // Duyệt không tự gỡ phân công: admin thấy danh sách chuyến bị trùng và tự đổi người ở trang Phân công tour.
    public class AdminDonXinNghiController : Controller
    {
        private readonly DuLichDbContext _context;

        public AdminDonXinNghiController(DuLichDbContext context)
        {
            _context = context;
        }

        private bool LaAdmin() => HttpContext.Session.GetString("role") == "admin";

        // loc: "cho" (mặc định) đơn chờ duyệt; "da" đơn đã xử lý; "tat" tất cả
        public IActionResult Index(string? loc)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");
            loc = loc is "da" or "tat" ? loc : "cho";

            var q = _context.DonXinNghis.AsNoTracking()
                .Include(d => d.NhanVien).ThenInclude(n => n!.TaiKhoan)
                .Include(d => d.NguoiDuyet)
                .AsQueryable();
            q = loc switch
            {
                "cho" => q.Where(d => d.TrangThai == TrangThaiDonNghi.ChoDuyet),
                "da" => q.Where(d => d.TrangThai != TrangThaiDonNghi.ChoDuyet),
                _ => q
            };
            var ds = (loc == "cho" ? q.OrderBy(d => d.TuNgay) : q.OrderByDescending(d => d.NgayGui)).ToList();

            ViewBag.Loc = loc;
            ViewBag.SoChoDuyet = _context.DonXinNghis.Count(d => d.TrangThai == TrangThaiDonNghi.ChoDuyet);
            var homNay = GioVN.HomNay;
            ViewBag.DangNghiHomNay = _context.DonXinNghis
                .Count(d => d.TrangThai == TrangThaiDonNghi.DaDuyet && d.TuNgay <= homNay && d.DenNgay >= homNay);
            // Chuyến bị trùng của đơn chờ duyệt và đơn đã duyệt
            ViewBag.ChuyenTrung = ds.Where(d => d.TrangThai is TrangThaiDonNghi.ChoDuyet or TrangThaiDonNghi.DaDuyet)
                .ToDictionary(d => d.Id, d => XuLyDonNghi.ChuyenTrung(_context, d.NhanVienId, d.TuNgay, d.DenNgay));
            return View(ds);
        }

        // Duyệt hoặc từ chối. Câu UPDATE có điều kiện: đơn nhân viên vừa rút hoặc admin khác vừa xử lý thì không đổi nữa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XuLy(int id, bool dongY, string? ghiChu)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            ghiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();
            if (ghiChu?.Length > 300) ghiChu = ghiChu[..300];
            if (!dongY && ghiChu == null)
            {
                TempData["error"] = "Từ chối đơn thì ghi lý do để nhân viên biết.";
                return RedirectToAction("Index");
            }

            var nguoiDuyet = HttpContext.Session.GetInt32("taiKhoanId");
            if (!nguoiDuyet.HasValue || !_context.TaiKhoans.Any(t => t.Id == nguoiDuyet.Value))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            var trangThai = dongY ? TrangThaiDonNghi.DaDuyet : TrangThaiDonNghi.TuChoi;
            int soDong = _context.Database.ExecuteSqlInterpolated($@"
                UPDATE DonXinNghis SET TrangThai = {trangThai}, NguoiDuyetId = {nguoiDuyet.Value},
                       NgayDuyet = {GioVN.BayGio}, GhiChuDuyet = {ghiChu}
                WHERE Id = {id} AND TrangThai = {TrangThaiDonNghi.ChoDuyet}");
            if (soDong == 0)
            {
                TempData["error"] = "Đơn này đã được xử lý hoặc nhân viên đã rút đơn.";
                return RedirectToAction("Index");
            }

            var don = _context.DonXinNghis.AsNoTracking().Include(d => d.NhanVien).ThenInclude(n => n!.TaiKhoan).First(d => d.Id == id);
            var trung = dongY ? XuLyDonNghi.ChuyenTrung(_context, don.NhanVienId, don.TuNgay, don.DenNgay) : new();
            TempData["success"] = !dongY
                ? $"Đã từ chối đơn của {don.NhanVien?.TaiKhoan?.HoTen}."
                : trung.Count == 0
                    ? $"Đã duyệt đơn nghỉ của {don.NhanVien?.TaiKhoan?.HoTen}."
                    : $"Đã duyệt đơn nghỉ của {don.NhanVien?.TaiKhoan?.HoTen}. Cần đổi người cho {trung.Count} chuyến: "
                      + string.Join(", ", trung.Select(c => c.TenTour + " " + c.NgayDi.ToString("dd/MM"))) + ".";
            if (dongY && trung.Count > 0) TempData["canDoiPhanCong"] = true;
            return RedirectToAction("Index");
        }
    }
}
