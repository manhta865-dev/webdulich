using Microsoft.AspNetCore.Mvc;
using webdulich.Models;
using System.Linq;

namespace webdulich.Controllers
{
    public class DiaDiemController : Controller
    {
        private readonly DuLichDbContext _context;

        public DiaDiemController(DuLichDbContext context)
        {
            _context = context;
        }

        public IActionResult DanhSach(string keyword)
        {
            var ds = _context.Tours.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim();
                ds = ds.Where(x => x.TenTour.Contains(keyword) || x.DiaDiem.Contains(keyword));
            }

            ViewBag.Keyword = keyword;
            return View(ds.ToList());
        }

        public IActionResult Index()
        {
            return RedirectToAction("DanhSach");
        }

        public IActionResult ChiTiet(int id)
        {
            var tour = _context.Tours.FirstOrDefault(x => x.Id == id);

            if (tour == null)
            {
                return NotFound();
            }

            return View(tour);
        }
    }
}