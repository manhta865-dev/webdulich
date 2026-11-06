using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Admin viết, sửa, đăng, gỡ và xóa bài viết
    public class AdminBaiVietController : Controller
    {
        private const long AnhToiDa = 5 * 1024 * 1024;

        private readonly DuLichDbContext _context;
        private readonly IWebHostEnvironment _moiTruong;

        public AdminBaiVietController(DuLichDbContext context, IWebHostEnvironment moiTruong)
        {
            _context = context;
            _moiTruong = moiTruong;
        }

        private bool LaAdmin() => HttpContext.Session.GetString("role") == "admin";

        public IActionResult Index(string? search)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var truyVan = _context.BaiViets.AsNoTracking().Include(x => x.TacGia).Include(x => x.Tour).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                truyVan = truyVan.Where(x => x.TieuDe.Contains(search));
            }

            ViewBag.Search = search;
            return View(truyVan.OrderByDescending(x => x.NgayDang ?? x.NgayTao).ToList());
        }

        public IActionResult Viet()
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            NapDanhSachTour(null);
            return View("Form", new BaiVietForm());
        }

        public IActionResult Sua(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var bai = _context.BaiViets.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (bai == null)
            {
                return NotFound();
            }

            NapDanhSachTour(bai.TourId);
            return View("Form", new BaiVietForm
            {
                Id = bai.Id,
                TieuDe = bai.TieuDe,
                TomTat = bai.TomTat,
                NoiDung = bai.NoiDung,
                TourId = bai.TourId,
                AnhBiaHienTai = bai.AnhBia,
                TrangThaiHienTai = bai.TrangThai
            });
        }

        // Một form cho cả viết mới và sửa. hanhDong: "dang" là đăng hoặc giữ đăng, "nhap" là lưu thành nháp.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Luu(BaiVietForm form, string? hanhDong)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");
            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            BaiViet? bai = null;
            if (form.Id.HasValue)
            {
                bai = _context.BaiViets.FirstOrDefault(x => x.Id == form.Id.Value);
                if (bai == null)
                {
                    return NotFound();
                }
            }
            form.AnhBiaHienTai = bai?.AnhBia;
            form.TrangThaiHienTai = bai?.TrangThai ?? BaiViet.Nhap;

            if (string.IsNullOrWhiteSpace(form.NoiDung) || BaiViet.TachKhoi(form.NoiDung).Count == 0)
            {
                ModelState.AddModelError(nameof(form.NoiDung), "Nhập nội dung bài viết.");
            }

            string? duoiAnh = null;
            if (form.TepAnhBia != null && !KiemTraAnh(form.TepAnhBia, out duoiAnh, out var loiAnh))
            {
                ModelState.AddModelError(nameof(form.TepAnhBia), loiAnh);
            }

            if (form.TourId.HasValue && !_context.Tours.Any(x => x.Id == form.TourId.Value))
            {
                ModelState.AddModelError(nameof(form.TourId), "Tour này không còn nữa, hãy chọn lại.");
            }

            if (!ModelState.IsValid)
            {
                NapDanhSachTour(form.TourId);
                return View("Form", form);
            }

            var bayGio = GioVN.BayGio;
            if (bai == null)
            {
                bai = new BaiViet
                {
                    NgayTao = bayGio,
                    TacGiaId = taiKhoanId.Value,
                    DuongDan = TaoDuongDanChuaDung(form.TieuDe)
                };
                _context.BaiViets.Add(bai);
            }

            bai.TieuDe = form.TieuDe.Trim();
            bai.TomTat = form.TomTat.Trim();
            bai.NoiDung = form.NoiDung.Trim();
            bai.TourId = form.TourId;

            bool dang = hanhDong == "dang";
            bool daDangTuTruoc = bai.TrangThai == BaiViet.DaDang;
            // Giữ ngày đăng cũ khi sửa bài đang đăng, để bài không nhảy lên đầu danh sách chỉ vì sửa lỗi chính tả
            if (dang && bai.TrangThai != BaiViet.DaDang)
            {
                bai.NgayDang = bayGio;
            }
            bai.TrangThai = dang ? BaiViet.DaDang : BaiViet.Nhap;

            string? anhCu = null;
            if (form.TepAnhBia != null)
            {
                anhCu = bai.AnhBia;
                bai.AnhBia = await LuuAnhAsync(form.TepAnhBia, duoiAnh!);
            }
            else if (form.XoaAnhBia)
            {
                anhCu = bai.AnhBia;
                bai.AnhBia = null;
            }

            try
            {
                _context.SaveChanges();
            }
            // Hai người tạo cùng tiêu đề đúng lúc: chỉ mục duy nhất của DuongDan chặn, không ghi đè bài của nhau
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                XoaTepAnh(bai.AnhBia);
                TempData["error"] = "Đường dẫn bài viết vừa bị trùng. Hãy bấm lưu lại.";
                NapDanhSachTour(form.TourId);
                return View("Form", form);
            }
            XoaTepAnh(anhCu);

            TempData["success"] = !dang
                ? $"Đã lưu nháp bài \"{bai.TieuDe}\"."
                : daDangTuTruoc ? $"Đã lưu thay đổi bài \"{bai.TieuDe}\"." : $"Đã đăng bài \"{bai.TieuDe}\".";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiTrangThai(int id, bool dang)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var bai = _context.BaiViets.FirstOrDefault(x => x.Id == id);
            if (bai == null)
            {
                return NotFound();
            }

            if (dang && bai.TrangThai != BaiViet.DaDang)
            {
                bai.NgayDang = GioVN.BayGio;
            }
            bai.TrangThai = dang ? BaiViet.DaDang : BaiViet.Nhap;
            _context.SaveChanges();

            TempData["success"] = dang ? $"Đã đăng bài \"{bai.TieuDe}\"." : $"Đã gỡ bài \"{bai.TieuDe}\" về nháp, khách không xem được nữa.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Xoa(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var bai = _context.BaiViets.FirstOrDefault(x => x.Id == id);
            if (bai == null)
            {
                return NotFound();
            }

            _context.BaiViets.Remove(bai);
            _context.SaveChanges();
            XoaTepAnh(bai.AnhBia);

            TempData["success"] = $"Đã xóa bài \"{bai.TieuDe}\".";
            return RedirectToAction("Index");
        }

        private void NapDanhSachTour(int? tourDangChon)
        {
            // Giữ cả tour đang gắn dù đã ngừng bán, để sửa bài không tự mất liên kết
            var dsTour = _context.Tours.AsNoTracking()
                .Where(x => !x.NgungBan || x.Id == tourDangChon)
                .OrderBy(x => x.TenTour)
                .Select(x => new { x.Id, x.TenTour })
                .ToList();
            ViewBag.DsTour = new SelectList(dsTour, "Id", "TenTour", tourDangChon);
        }

        private string TaoDuongDanChuaDung(string tieuDe)
        {
            var goc = BaiViet.TaoDuongDan(tieuDe);
            var duongDan = goc;
            for (int so = 2; _context.BaiViets.Any(x => x.DuongDan == duongDan); so++)
            {
                duongDan = $"{goc}-{so}";
            }
            return duongDan;
        }

        // Chỉ nhận ảnh thật: kiểm tra đuôi tệp và mấy byte đầu, không tin tên tệp người dùng gửi
        private static bool KiemTraAnh(IFormFile tep, out string? duoi, out string loi)
        {
            duoi = null;
            loi = string.Empty;
            if (tep.Length == 0)
            {
                loi = "Tệp ảnh trống, hãy chọn tệp khác.";
                return false;
            }
            if (tep.Length > AnhToiDa)
            {
                loi = "Ảnh quá lớn, tối đa 5 MB.";
                return false;
            }

            var dau = new byte[12];
            using (var luong = tep.OpenReadStream())
            {
                luong.ReadExactly(dau, 0, Math.Min(12, (int)tep.Length));
            }

            bool laJpg = dau[0] == 0xFF && dau[1] == 0xD8 && dau[2] == 0xFF;
            bool laPng = dau[0] == 0x89 && dau[1] == 0x50 && dau[2] == 0x4E && dau[3] == 0x47;
            bool laWebp = dau[0] == 0x52 && dau[1] == 0x49 && dau[2] == 0x46 && dau[3] == 0x46
                && dau[8] == 0x57 && dau[9] == 0x45 && dau[10] == 0x42 && dau[11] == 0x50;

            duoi = laJpg ? ".jpg" : laPng ? ".png" : laWebp ? ".webp" : null;
            if (duoi == null)
            {
                loi = "Chỉ nhận ảnh JPG, PNG hoặc WebP.";
                return false;
            }
            return true;
        }

        private string ThuMucAnh()
        {
            var thuMuc = Path.Combine(_moiTruong.WebRootPath, "images", "baiviet");
            Directory.CreateDirectory(thuMuc);
            return thuMuc;
        }

        private async Task<string> LuuAnhAsync(IFormFile tep, string duoi)
        {
            var ten = Guid.NewGuid().ToString("N") + duoi;
            using var luong = new FileStream(Path.Combine(ThuMucAnh(), ten), FileMode.CreateNew);
            await tep.CopyToAsync(luong);
            return ten;
        }

        private void XoaTepAnh(string? ten)
        {
            if (string.IsNullOrEmpty(ten))
            {
                return;
            }
            try
            {
                // GetFileName để chắc chắn không ra khỏi thư mục ảnh bài viết
                var duongDan = Path.Combine(ThuMucAnh(), Path.GetFileName(ten));
                if (System.IO.File.Exists(duongDan))
                {
                    System.IO.File.Delete(duongDan);
                }
            }
            catch (IOException)
            {
                // Tệp đang bị khóa thì để lại, chỉ tốn chỗ chứ không ảnh hưởng dữ liệu
            }
        }
    }
}
