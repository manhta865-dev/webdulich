using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Admin xem và ẩn đánh giá tour (RB-43). Không có nút xóa: đánh giá vi phạm thì ẩn, giữ lại làm bằng chứng.
    public class AdminDanhGiaController : Controller
    {
        private readonly DuLichDbContext _context;

        public AdminDanhGiaController(DuLichDbContext context)
        {
            _context = context;
        }

        private bool LaAdmin() => HttpContext.Session.GetString("role") == "admin";

        public IActionResult Index(int? sao, string? loc)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var q = _context.DanhGiaTours.AsNoTracking()
                .Include(d => d.TaiKhoan)
                .Include(d => d.LichKhoiHanhTour).ThenInclude(l => l!.Tour)
                .AsQueryable();
            if (sao is >= 1 and <= 5) q = q.Where(d => d.SoSao == sao);
            if (loc == "an") q = q.Where(d => !d.DangHien);

            ViewBag.Sao = sao;
            ViewBag.Loc = loc;
            ViewBag.TongSo = _context.DanhGiaTours.Count();
            ViewBag.SoAn = _context.DanhGiaTours.Count(d => !d.DangHien);
            ViewBag.TrungBinh = _context.DanhGiaTours.Where(d => d.DangHien).Select(d => (double?)d.SoSao).Average() ?? 0;
            return View(q.OrderByDescending(d => d.NgaySua ?? d.NgayTao).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiHienThi(int id, bool hien)
        {
            if (!LaAdmin()) return RedirectToAction("DangNhap", "TaiKhoan");

            var dg = _context.DanhGiaTours.Find(id);
            if (dg == null) return NotFound();
            dg.DangHien = hien;
            _context.SaveChanges();
            TempData["success"] = hien ? "Đã hiện lại đánh giá trên trang tour." : "Đã ẩn đánh giá khỏi trang tour.";
            return RedirectToAction("Index");
        }
    }
}
