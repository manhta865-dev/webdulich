using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Admin thêm video bằng link YouTube, sửa, ẩn hiện, đổi thứ tự, xóa
    public class AdminVideoController : Controller
    {
        private readonly DuLichDbContext _context;

        public AdminVideoController(DuLichDbContext context)
        {
            _context = context;
        }

        private bool LaAdmin() => HttpContext.Session.GetString("role") == "admin";

        public IActionResult Index()
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            return View(_context.Videos.AsNoTracking().Include(x => x.Tour).OrderBy(x => x.ThuTu).ThenBy(x => x.Id).ToList());
        }

        public IActionResult Them()
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            NapDanhSachTour(null);
            return View("Form", new VideoForm());
        }

        public IActionResult Sua(int id)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var video = _context.Videos.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (video == null)
            {
                return NotFound();
            }

            NapDanhSachTour(video.TourId);
            return View("Form", new VideoForm
            {
                Id = video.Id,
                Link = video.LinkXem,
                TieuDe = video.TieuDe,
                MoTa = video.MoTa,
                TourId = video.TourId,
                DangHien = video.DangHien
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Luu(VideoForm form)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            Video? video = null;
            if (form.Id.HasValue)
            {
                video = _context.Videos.FirstOrDefault(x => x.Id == form.Id.Value);
                if (video == null)
                {
                    return NotFound();
                }
            }

            var ma = Video.DocMaYouTube(form.Link);
            if (!string.IsNullOrWhiteSpace(form.Link) && ma == null)
            {
                ModelState.AddModelError(nameof(form.Link), "Link chưa đúng. Hãy dán link video YouTube, ví dụ https://www.youtube.com/watch?v=abcdefghijk.");
            }
            else if (ma != null && _context.Videos.Any(x => x.MaYouTube == ma && x.Id != form.Id))
            {
                ModelState.AddModelError(nameof(form.Link), "Video này đã có trong danh sách.");
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

            if (video == null)
            {
                video = new Video
                {
                    NgayTao = GioVN.BayGio,
                    // Video mới xếp cuối danh sách
                    ThuTu = (_context.Videos.Max(x => (int?)x.ThuTu) ?? 0) + 1
                };
                _context.Videos.Add(video);
            }

            video.MaYouTube = ma!;
            video.TieuDe = form.TieuDe.Trim();
            video.MoTa = string.IsNullOrWhiteSpace(form.MoTa) ? null : form.MoTa.Trim();
            video.TourId = form.TourId;
            video.DangHien = form.DangHien;

            try
            {
                _context.SaveChanges();
            }
            // Hai người thêm cùng một video đúng lúc: chỉ mục duy nhất chặn
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
            {
                _context.ChangeTracker.Clear();
                ModelState.AddModelError(nameof(form.Link), "Video này đã có trong danh sách.");
                NapDanhSachTour(form.TourId);
                return View("Form", form);
            }

            TempData["success"] = $"Đã lưu video \"{video.TieuDe}\".";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiHienThi(int id, bool hien)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var video = _context.Videos.FirstOrDefault(x => x.Id == id);
            if (video == null)
            {
                return NotFound();
            }

            video.DangHien = hien;
            _context.SaveChanges();

            TempData["success"] = hien ? $"Đã hiện video \"{video.TieuDe}\"." : $"Đã ẩn video \"{video.TieuDe}\", khách không xem được nữa.";
            return RedirectToAction("Index");
        }

        // Đổi chỗ với video liền trên hoặc liền dưới. Đánh số lại cả danh sách để không bị kẹt khi hai video trùng thứ tự.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiThuTu(int id, bool len)
        {
            if (!LaAdmin())
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var ds = _context.Videos.OrderBy(x => x.ThuTu).ThenBy(x => x.Id).ToList();
            int viTri = ds.FindIndex(x => x.Id == id);
            if (viTri < 0)
            {
                return NotFound();
            }

            int viTriMoi = len ? viTri - 1 : viTri + 1;
            if (viTriMoi >= 0 && viTriMoi < ds.Count)
            {
                (ds[viTri], ds[viTriMoi]) = (ds[viTriMoi], ds[viTri]);
                for (int i = 0; i < ds.Count; i++)
                {
                    ds[i].ThuTu = i + 1;
                }
                _context.SaveChanges();
            }
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

            var video = _context.Videos.FirstOrDefault(x => x.Id == id);
            if (video == null)
            {
                return NotFound();
            }

            _context.Videos.Remove(video);
            _context.SaveChanges();

            TempData["success"] = $"Đã xóa video \"{video.TieuDe}\" khỏi danh sách. Video trên YouTube không bị ảnh hưởng.";
            return RedirectToAction("Index");
        }

        private void NapDanhSachTour(int? tourDangChon)
        {
            var dsTour = _context.Tours.AsNoTracking()
                .Where(x => !x.NgungBan || x.Id == tourDangChon)
                .OrderBy(x => x.TenTour)
                .Select(x => new { x.Id, x.TenTour })
                .ToList();
            ViewBag.DsTour = new SelectList(dsTour, "Id", "TenTour", tourDangChon);
        }
    }
}
