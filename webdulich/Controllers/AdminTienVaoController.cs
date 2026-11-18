using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using webdulich.Services;

namespace webdulich.Controllers
{
    // Admin xem các khoản tiền vào do SePay báo về (RB-48): khoản nào đã tự xác nhận ghế/đơn nào, khoản nào cần xử lý tay.
    // Khoản không khớp: admin đối chiếu, xác nhận tay ở trang đơn hoặc hoàn tiền ngoài hệ thống, rồi đánh dấu đã xử lý.
    // Trang giả lập "gửi tiền vào giả" chỉ hiện khi ThanhToanTuDong:ChoPhepGiaLap = true (bật khi thử trên máy).
    public class AdminTienVaoController : Controller
    {
        private readonly DuLichDbContext _context;
        private readonly IConfiguration _cauHinh;

        public AdminTienVaoController(DuLichDbContext context, IConfiguration cauHinh)
        {
            _context = context;
            _cauHinh = cauHinh;
        }

        private bool LaAdmin() => HttpContext.Session.GetString("role") == "admin";

        private bool ChoPhepGiaLap => _cauHinh.GetValue("ThanhToanTuDong:ChoPhepGiaLap", false);

        public IActionResult Index(bool canXuLy = false)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var q = _context.GiaoDichNganHangs.AsNoTracking()
                .Include(x => x.NguoiXuLy)
                .Include(x => x.GiaoDichThanhToan).ThenInclude(g => g!.HanhKhachDatTour)
                .AsQueryable();
            if (canXuLy)
            {
                q = q.Where(x => x.KetQua != KetQuaTienVao.DaKhop && x.NguoiXuLyId == null);
            }

            ViewBag.CanXuLy = canXuLy;
            ViewBag.SoCanXuLy = _context.GiaoDichNganHangs.Count(x => x.KetQua != KetQuaTienVao.DaKhop && x.NguoiXuLyId == null);
            ViewBag.DangBat = !string.IsNullOrWhiteSpace(_cauHinh["ThanhToanTuDong:SePayApiKey"]);
            ViewBag.ChoPhepGiaLap = ChoPhepGiaLap;
            // Mã đơn để mở trang chi tiết đơn từ khoản đã khớp
            var ds = q.OrderByDescending(x => x.Id).Take(200).ToList();
            var dsDonId = ds.Select(x => x.GiaoDichThanhToan?.DatTourId ?? x.GiaoDichThanhToan?.HanhKhachDatTour?.DatTourId)
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            ViewBag.MaDon = _context.DatTours.AsNoTracking().Where(d => dsDonId.Contains(d.Id))
                .ToDictionary(d => d.Id, d => d.MaChuyenDi);
            return View(ds);
        }

        // Đánh dấu một khoản không khớp là đã xử lý tay (đã xác nhận tay ở trang đơn, hoặc đã hoàn tiền)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DaXuLy(int id, string? ghiChu)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            var adminId = HttpContext.Session.GetInt32("taiKhoanId");
            ghiChu = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();
            if (ghiChu?.Length > 300)
            {
                ghiChu = ghiChu[..300];
            }

            int soDong = adminId.HasValue
                ? _context.Database.ExecuteSqlInterpolated($@"
                    UPDATE GiaoDichNganHangs SET NguoiXuLyId = {adminId.Value}, NgayXuLy = {GioVN.BayGio}, GhiChuXuLy = {ghiChu}
                    WHERE Id = {id} AND KetQua <> {KetQuaTienVao.DaKhop} AND NguoiXuLyId IS NULL")
                : 0;
            TempData[soDong > 0 ? "success" : "error"] = soDong > 0
                ? "Đã đánh dấu khoản tiền vào là đã xử lý."
                : "Khoản này đã tự khớp hoặc đã được xử lý trước đó.";
            return RedirectToAction("Index", new { canXuLy = true });
        }

        // Tạo một khoản tiền vào giả để thử luồng tự xác nhận khi chưa có giao dịch thật (chỉ khi bật ChoPhepGiaLap)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GiaLap(string? noiDung, decimal? soTien)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }
            if (!ChoPhepGiaLap)
            {
                return NotFound();
            }
            if (string.IsNullOrWhiteSpace(noiDung) || soTien is not > 0)
            {
                TempData["error"] = "Nhập nội dung chuyển khoản và số tiền lớn hơn 0.";
                return RedirectToAction("Index");
            }

            var ketQua = DoiChieuTienVao.Nhan(_context, new TienVao(
                NguonTienVao.GiaLap,
                "GL-" + Guid.NewGuid().ToString("N"),
                "Giả lập",
                _cauHinh["ThanhToan:SoTaiKhoan"],
                GioVN.BayGio,
                soTien.Value,
                noiDung.Trim(),
                null));

            TempData[ketQua.Dong.KetQua == KetQuaTienVao.DaKhop ? "success" : "error"] =
                $"Tiền vào giả {soTien.Value:N0} VNĐ, nội dung \"{noiDung.Trim()}\": {KetQuaTienVao.Ten(ketQua.Dong.KetQua)}.";
            return RedirectToAction("Index");
        }
    }
}
