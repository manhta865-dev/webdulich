using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using Microsoft.AspNetCore.Http;

namespace webdulich.Controllers
{
    public class AdminController : Controller
    {
        private readonly DuLichDbContext _context;

        public AdminController(DuLichDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("role");

            if (role != "admin")
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var tours = _context.Tours.ToList();
            return View(tours);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Tour tour)
        {
            if (tour.HinhFile == null)
            {
                ModelState.AddModelError("HinhFile", "Vui lòng chọn hình ảnh");
                return View(tour);
            }

            if (ModelState.IsValid)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "dulich");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = Guid.NewGuid().ToString() + "_" + tour.HinhFile.FileName;
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await tour.HinhFile.CopyToAsync(stream);
                }

                // Lưu đường dẫn để hiển thị ảnh ngoài View
                tour.HinhAnh =  fileName;

                _context.Tours.Add(tour);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(tour);
        }

        public IActionResult Edit(int id)
        {
            var tour = _context.Tours.Find(id);

            if (tour == null)
            {
                return NotFound();
            }

            return View("Sua", tour);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Tour tour)
        {
            if (!ModelState.IsValid)
            {
                return View("Sua", tour);
            }

            var tourCu = _context.Tours.Find(tour.Id);

            if (tourCu == null)
            {
                return NotFound();
            }

            tourCu.TenTour = tour.TenTour;
            tourCu.DiaDiem = tour.DiaDiem;
            tourCu.Gia = tour.Gia;
            tourCu.MoTa = tour.MoTa;

            if (tour.HinhFile != null)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "dulich");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = Guid.NewGuid().ToString() + "_" + tour.HinhFile.FileName;
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await tour.HinhFile.CopyToAsync(stream);
                }

                tourCu.HinhAnh =  fileName;
            }

            _context.Tours.Update(tourCu);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var tour = _context.Tours.Find(id);

            if (tour == null)
            {
                return NotFound();
            }

            _context.Tours.Remove(tour);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult DonDatTour()
        {
            var list = _context.DatTours
                .Include(x => x.Tour)
                .ToList();

            ViewBag.Dem = list.Count;

            return View(list);
        }
        public IActionResult DonDatTourTheoTour(int id)
        {
            var list = _context.DatTours
                .Include(x => x.Tour)
                .Where(x => x.TourId == id)
                .ToList();

            return View("DonDatTour", list);
        }
        public IActionResult SuaDonDatTour(int id)
        {
            var role = HttpContext.Session.GetString("role");

            if (role != "admin")
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var datTour = _context.DatTours.FirstOrDefault(x => x.Id == id);

            if (datTour == null)
            {
                return NotFound();
            }

            ViewBag.Tours = _context.Tours.ToList();
            return View(datTour);
        }

        [HttpPost]
        public IActionResult SuaDonDatTour(DatTours model)
        {
            var role = HttpContext.Session.GetString("role");

            if (role != "admin")
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var datTour = _context.DatTours.FirstOrDefault(x => x.Id == model.Id);

            if (datTour == null)
            {
                return NotFound();
            }

            datTour.TenKhach = model.TenKhach;
            datTour.DienThoai = model.DienThoai;
            datTour.SoNguoi = model.SoNguoi;
            datTour.NgayDi = model.NgayDi;
            datTour.TourId = model.TourId;

            _context.SaveChanges();

            return RedirectToAction("DonDatTour");
            TempData["success"] = "Cập nhật thành công!";
            return RedirectToAction("DonDatTour");
        }

        public IActionResult XoaDonDatTour(int id)
        {
            var role = HttpContext.Session.GetString("role");

            if (role != "admin")
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var datTour = _context.DatTours.FirstOrDefault(x => x.Id == id);

            if (datTour == null)
            {
                return NotFound();
            }

            _context.DatTours.Remove(datTour);
            _context.SaveChanges();

            return RedirectToAction("DonDatTour");

        }


    }

}
