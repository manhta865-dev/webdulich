using Microsoft.AspNetCore.Mvc;
using webdulich.Models;
using System.Linq;

namespace webdulich.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly DuLichDbContext _context;

        public TaiKhoanController(DuLichDbContext context)
        {
            _context = context;
        }

        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DangKy(string hoten, string taikhoan, string matkhau)
        {
            if (string.IsNullOrEmpty(hoten) || string.IsNullOrEmpty(taikhoan) || string.IsNullOrEmpty(matkhau))
            {
                ViewBag.Loi = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            TaiKhoan tk = new TaiKhoan();
            tk.HoTen = hoten;
            tk.TaiKhoanDangNhap = taikhoan;
            tk.MatKhau = matkhau;
            tk.VaiTro = "user";

            _context.TaiKhoans.Add(tk);
            _context.SaveChanges();

            return RedirectToAction("DangNhap");
        }

        public IActionResult DangNhap()
        {
            return View();
        }

        [HttpPost]
        public IActionResult DangNhap(string taikhoan, string matkhau)
        {
            var user = _context.TaiKhoans
                .FirstOrDefault(x => x.TaiKhoanDangNhap == taikhoan && x.MatKhau == matkhau);

            if (user != null)
            {
                HttpContext.Session.SetString("user", user.HoTen);
                HttpContext.Session.SetString("role", user.VaiTro);

                if (user.VaiTro == "admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    return RedirectToAction("Index", "TrangChu");
                }
            }

            ViewBag.Loi = "Sai tài khoản hoặc mật khẩu";
            return View();
        }

        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "TrangChu");
        }
    }
}