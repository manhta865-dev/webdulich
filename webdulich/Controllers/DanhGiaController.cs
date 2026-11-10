using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Khách viết hoặc sửa đánh giá cho chuyến đã đi xong (RB-43). Vào từ trang Lịch sử đặt tour.
    public class DanhGiaController : Controller
    {
        private readonly DuLichDbContext _context;

        public DanhGiaController(DuLichDbContext context)
        {
            _context = context;
        }

        // Chỉ khách đã đăng nhập; admin và nhân viên không đánh giá
        private int? KhachDangNhap()
        {
            var vaiTro = HttpContext.Session.GetString("role");
            return vaiTro == "user" ? HttpContext.Session.GetInt32("taiKhoanId") : null;
        }

        private bool DuocDanhGia(int taiKhoanId, int lichId) =>
            DanhGiaDoc.LichDuocDanhGia(_context, taiKhoanId).Contains(lichId);

        [HttpGet]
        public IActionResult Viet(int lichId)
        {
            var tk = KhachDangNhap();
            if (tk == null) return RedirectToAction("DangNhap", "TaiKhoan");
            if (!DuocDanhGia(tk.Value, lichId)) return NotFound();

            var daCo = _context.DanhGiaTours.AsNoTracking().FirstOrDefault(d => d.TaiKhoanId == tk && d.LichKhoiHanhTourId == lichId);
            NapThongTinChuyen(lichId, daCo);
            return View(new DanhGiaForm { LichId = lichId, SoSao = daCo?.SoSao ?? 0, NoiDung = daCo?.NoiDung });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Viet(DanhGiaForm form)
        {
            var tk = KhachDangNhap();
            if (tk == null) return RedirectToAction("DangNhap", "TaiKhoan");
            if (!DuocDanhGia(tk.Value, form.LichId)) return NotFound();

            var daCo = _context.DanhGiaTours.FirstOrDefault(d => d.TaiKhoanId == tk && d.LichKhoiHanhTourId == form.LichId);
            if (!ModelState.IsValid)
            {
                NapThongTinChuyen(form.LichId, daCo);
                return View(form);
            }

            var noiDung = string.IsNullOrWhiteSpace(form.NoiDung) ? null : form.NoiDung.Trim();
            if (daCo == null)
            {
                _context.DanhGiaTours.Add(new DanhGiaTour
                {
                    LichKhoiHanhTourId = form.LichId,
                    TaiKhoanId = tk.Value,
                    SoSao = form.SoSao,
                    NoiDung = noiDung,
                    NgayTao = GioVN.BayGio
                });
            }
            else
            {
                // Sửa lại vẫn giữ quyết định ẩn của admin (khách không tự bỏ ẩn được)
                daCo.SoSao = form.SoSao;
                daCo.NoiDung = noiDung;
                daCo.NgaySua = GioVN.BayGio;
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                // Hai lần gửi cùng lúc: UNIQUE (TaiKhoanId, LichKhoiHanhTourId) chặn bản thứ hai
                TempData["Loi"] = "Đánh giá của bạn đã được lưu trước đó. Bạn có thể mở lại để sửa.";
                return RedirectToAction("LichSu", "DatTour");
            }

            TempData["ThanhCong"] = daCo == null ? "Cảm ơn bạn đã đánh giá chuyến đi." : "Đã cập nhật đánh giá của bạn.";
            return RedirectToAction("LichSu", "DatTour");
        }

        private void NapThongTinChuyen(int lichId, DanhGiaTour? daCo)
        {
            var lich = _context.LichKhoiHanhTours.AsNoTracking().Include(l => l.Tour).First(l => l.Id == lichId);
            ViewBag.TenTour = lich.Tour?.TenTour ?? "";
            ViewBag.Anh = lich.Tour?.HinhAnh;
            ViewBag.NgayDi = lich.NgayKhoiHanh;
            ViewBag.DaCo = daCo != null;
            ViewBag.BiAn = daCo != null && !daCo.DangHien;
        }
    }
}
