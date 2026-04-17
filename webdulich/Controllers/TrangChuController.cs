using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using System.Linq;

namespace webdulich.Controllers
{
    public class TrangChuController : Controller
    {
        private readonly DuLichDbContext _context;

        public TrangChuController(DuLichDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Tour nổi bật ở giữa trang
            var tours = _context.Tours.Take(3).ToList();

            // Top khách hàng đặt tour nhiều nhất
            var topKhach = _context.DatTours
                .GroupBy(x => x.TenKhach)
                .Select(g => new
                {
                    TenKhach = g.Key,
                    SoLan = g.Count()
                })
                .OrderByDescending(x => x.SoLan)
                .Take(5)
                .ToList();

            // Top tour được đặt nhiều nhất
            var topTour = _context.DatTours
                .Include(x => x.Tour)
                .GroupBy(x => x.Tour!.TenTour)
                .Select(g => new
                {
                    TenTour = g.Key,
                    SoLan = g.Count()
                })
                .OrderByDescending(x => x.SoLan)
                .Take(5)
                .ToList();

            ViewBag.TopKhach = topKhach;
            ViewBag.TopTour = topTour;

            return View(tours);
        }
    }
}