using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using System.Linq;

namespace webdulich.Controllers
{
    public class DatTourController : Controller
    {
        private readonly DuLichDbContext _context;

        public DatTourController(DuLichDbContext context)
        {
            _context = context;
        }

        // GET: Hiển thị form đặt tour
        public IActionResult DatTour(int id)
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }

            var tour = _context.Tours.FirstOrDefault(x => x.Id == id);
            if (tour == null)
            {
                return NotFound();
            }

            ViewBag.Tour = tour;

            DatTours model = new DatTours
            {
                TourId = tour.Id,
                TenKhach = user
            };

            return View(model);
        }

        // POST: Lưu đơn đặt tour
        [HttpPost]
        public IActionResult DatTour(DatTours model)
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }

            var tour = _context.Tours.FirstOrDefault(x => x.Id == model.TourId);
            if (tour == null)
            {
                return NotFound();
            }

            model.Id = 0;
            model.TenKhach = user;

            if (ModelState.IsValid)
            {
                _context.DatTours.Add(model);
                _context.SaveChanges();
                return RedirectToAction("ThanhCong");
            }

            ViewBag.Tour = tour;
            return View(model);
        }

        public IActionResult ThanhCong()
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }

            return View();
        }

        public IActionResult LichSu()
        {
            var user = HttpContext.Session.GetString("user");
            var role = HttpContext.Session.GetString("role");

            if (string.IsNullOrEmpty(user))
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            if (role == "admin")
            {
                return RedirectToAction("Index", "TrangChu");
            }

            var ds = _context.DatTours
                .Include(x => x.Tour)
                .Where(x => x.TenKhach == user)
                .OrderByDescending(x => x.NgayDi)
                .ToList();

            return View(ds);
        }
    }
}