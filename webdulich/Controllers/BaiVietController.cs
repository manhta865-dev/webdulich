using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Khách đọc bài viết. Chỉ bài đã đăng mới hiện; admin xem trước được bản nháp.
    public class BaiVietController : Controller
    {
        private const int SoBaiMoiTrang = 9;

        private readonly DuLichDbContext _context;

        public BaiVietController(DuLichDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int trang = 1)
        {
            var daDang = _context.BaiViets.AsNoTracking().Where(x => x.TrangThai == BaiViet.DaDang);
            int tongBai = daDang.Count();
            int tongTrang = Math.Max(1, (int)Math.Ceiling(tongBai / (double)SoBaiMoiTrang));
            trang = Math.Clamp(trang, 1, tongTrang);

            ViewBag.Trang = trang;
            ViewBag.TongTrang = tongTrang;
            return View(daDang.OrderByDescending(x => x.NgayDang).ThenByDescending(x => x.Id)
                .Skip((trang - 1) * SoBaiMoiTrang).Take(SoBaiMoiTrang).ToList());
        }

        [HttpGet("BaiViet/{duongDan}")]
        public IActionResult ChiTiet(string duongDan)
        {
            var bai = _context.BaiViets.AsNoTracking().Include(x => x.Tour)
                .FirstOrDefault(x => x.DuongDan == duongDan);
            bool laAdmin = HttpContext.Session.GetString("role") == "admin";
            if (bai == null || (bai.TrangThai != BaiViet.DaDang && !laAdmin))
            {
                return NotFound();
            }

            ViewBag.BaiKhac = _context.BaiViets.AsNoTracking()
                .Where(x => x.TrangThai == BaiViet.DaDang && x.Id != bai.Id)
                .OrderByDescending(x => x.NgayDang)
                .Take(3)
                .ToList();
            return View(bai);
        }
    }
}
