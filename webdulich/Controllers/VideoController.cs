using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    // Khách xem các video đang hiện
    public class VideoController : Controller
    {
        private readonly DuLichDbContext _context;

        public VideoController(DuLichDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.Videos.AsNoTracking()
                .Where(x => x.DangHien)
                .OrderBy(x => x.ThuTu).ThenBy(x => x.Id)
                .ToList());
        }
    }
}
