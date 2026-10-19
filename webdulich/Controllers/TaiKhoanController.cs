using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using webdulich.Models;
using System.Text.RegularExpressions;

namespace webdulich.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly DuLichDbContext _context;

        public TaiKhoanController(DuLichDbContext context)
        {
            _context = context;
        }
        // Hàm này dùng để lưu ảnh đại diện của người dùng.
        private async Task<string> LuuAnhDaiDienAsync(IFormFile file)
        {
            // TẠO THƯ MỤC ẢNH 
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatar"); // ĐƯỜNG DẪN

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Ảnh đại diện chỉ nhận file JPG, JPEG, PNG hoặc WEBP.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("Ảnh đại diện không được vượt quá 5MB.");
            }

            string fileName = $"{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        //Hàm này dùng để lưu thông tin đăng nhập vào Session.
        private void LuuSessionDangNhap(TaiKhoan user)
        {
            // Lưu tên người dùng.
            HttpContext.Session.SetString("user", user.HoTen ?? "");
            HttpContext.Session.SetString("role", user.VaiTro ?? "user");
            HttpContext.Session.SetInt32("taiKhoanId", user.Id);

            string avatar = user.AnhDaiDien ?? "";
            HttpContext.Session.SetString("avatar", avatar);
            HttpContext.Session.SetString("anhDaiDien", avatar);
        }
        // Dùng để mở giao diện đăng ký.
        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }
        // Dùng khi người dùng bấm nút Đăng ký.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKy(TaiKhoan tk)
        {
            // Nếu chưa nhập họ tên thì báo lỗi.
            if (string.IsNullOrWhiteSpace(tk.HoTen))
            {
                ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên!");
            }
            // Nếu chưa nhập email hoặc số điện thoại thì báo lỗi.
            if (string.IsNullOrWhiteSpace(tk.TaiKhoanDangNhap))
            {
                ModelState.AddModelError("TaiKhoanDangNhap", "Vui lòng nhập email hoặc số điện thoại!");
            }
            else
            {
                // Kiểm tra định dạng email/số điện thoại
                string taiKhoan = tk.TaiKhoanDangNhap.Trim();

                bool laEmail = Regex.IsMatch(taiKhoan, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                bool laSoDienThoai = Regex.IsMatch(taiKhoan, @"^0\d{9}$");

                if (!laEmail && !laSoDienThoai)
                {
                    ModelState.AddModelError("TaiKhoanDangNhap", "Vui lòng nhập đúng email hoặc số điện thoại 10 số!");
                }
                else if (_context.TaiKhoans.Any(x => x.TaiKhoanDangNhap == taiKhoan)) //Kiểm tra trùng tài khoản
                {
                    ModelState.AddModelError("TaiKhoanDangNhap", "Email hoặc số điện thoại này đã được đăng ký!");
                }
            }
            // Nếu chưa nhập mật khẩu thì báo lỗi.
            if (string.IsNullOrWhiteSpace(tk.MatKhau))
            {
                ModelState.AddModelError("MatKhau", "Vui lòng nhập mật khẩu!");
            }
            // Nếu mật khẩu dưới 6 ký tự thì báo lỗi.
            else if (tk.MatKhau.Length < 6)
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu phải có ít nhất 6 ký tự!");
            }
          //  Trả lại trang đăng ký kèm thông báo lỗi.
            if (!ModelState.IsValid)
            {
                return View(tk);
            }

            tk.HoTen = tk.HoTen.Trim();
            tk.TaiKhoanDangNhap = tk.TaiKhoanDangNhap.Trim();
            tk.MatKhau = BCrypt.Net.BCrypt.HashPassword(tk.MatKhau.Trim()); // Mã hóa mật khẩu
            tk.VaiTro = "user"; // Tài khoản tự đăng ký mặc định là user.
            tk.NgayTao = DateTime.Now;
            tk.AnhDaiDien = "";
            // Thêm tài khoản mới vào bảng TaiKhoans.
            _context.TaiKhoans.Add(tk);
            _context.SaveChanges();

            TempData["ThanhCong"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("DangNhap");
        }
        // Dùng để mở trang đăng nhập.
        [HttpGet]
        public IActionResult DangNhap()
        {
            return View();
        }
        // Dùng khi người dùng bấm Đăng nhập.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangNhap(string taiKhoanDangNhap, string matKhau)
        {
            if (string.IsNullOrWhiteSpace(taiKhoanDangNhap))
            {
                ModelState.AddModelError("taiKhoanDangNhap", "Vui lòng nhập email hoặc số điện thoại!");
            }

            if (string.IsNullOrWhiteSpace(matKhau))
            {
                ModelState.AddModelError("matKhau", "Vui lòng nhập mật khẩu!");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            string taiKhoan = taiKhoanDangNhap.Trim();
            // Tìm tài khoản trong database theo email hoặc số điện thoại.
            var user = _context.TaiKhoans.FirstOrDefault(x => x.TaiKhoanDangNhap == taiKhoan);

            if (user == null || !BCrypt.Net.BCrypt.Verify(matKhau, user.MatKhau)) // có 2 trường hơp lỗi BCrypt.Verify() dùng để so sánh mật khẩu người dùng nhập với mật khẩu đã mã hóa trong database.
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không đúng!");
                return View();
            }
            // Lưu session đăng nhập
            LuuSessionDangNhap(user);
            // điều hướng theo vai trò 
            if (user.VaiTro == "admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            if (user.VaiTro == "huongdanvien" || user.VaiTro == "taixe")
            {
                return RedirectToAction("LichLamViec", "NhanVien");
            }

            return RedirectToAction("Index", "TrangChu");
        }
        // đăng xuất 
        public IActionResult DangXuat()
        {
            HttpContext.Session.Clear(); // Session.Clear() xóa toàn bộ thông tin đăng nhập.
            return RedirectToAction("DangNhap");
        }
        // Dùng để mở trang sửa hồ sơ.
        [HttpGet]
        public IActionResult SuaHoSo()
        {
            // Nếu chưa đăng nhập thì chuyển về đăng nhập.
            int? taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");

            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap");
            }
            // var user = _context.TaiKhoans.FirstOrDefault(x => x.Id == taiKhoanId.Value);
            var user = _context.TaiKhoans.FirstOrDefault(x => x.Id == taiKhoanId.Value);

            if (user == null)
            {
                return RedirectToAction("DangNhap");
            }
            // Xóa mật khẩu trước khi đưa ra giao diện để không hiện chuỗi mã hóa.
            user.MatKhau = "";
            return View(user);
        }
        // Dùng khi người dùng bấm Cập nhật hồ sơ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaHoSo(TaiKhoan model)
        {
            //Dựa vào Session để biết đang sửa hồ sơ của ai.
            int? taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");

            if (!taiKhoanId.HasValue)
            {
                return RedirectToAction("DangNhap");
            }

            var user = _context.TaiKhoans.FirstOrDefault(x => x.Id == taiKhoanId.Value);

            if (user == null)
            {
                return RedirectToAction("DangNhap");
            }
            // Lưu lại tên cũ và ảnh cũ.
            string tenCu = user.HoTen;
            string anhCu = user.AnhDaiDien ?? "";
            // Không cho để trống họ tên.
            if (string.IsNullOrWhiteSpace(model.HoTen))
            {
                ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên!");
                model.AnhDaiDien = anhCu;
                model.MatKhau = "";
                return View(model);
            }
            //Cập nhật họ tên
            user.HoTen = model.HoTen.Trim();
            // Nếu người dùng nhập mật khẩu mới thì mới đổi.
            if (!string.IsNullOrWhiteSpace(model.MatKhau))
            {
                if (model.MatKhau.Length < 6)
                {
                    ModelState.AddModelError("MatKhau", "Mật khẩu phải có ít nhất 6 ký tự!");
                    model.AnhDaiDien = anhCu;
                    model.MatKhau = "";
                    return View(model);
                }
                // Mã hóa mật khẩu mới
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhau.Trim());
            }
            // Nếu có chọn ảnh mới thì gọi hàm lưu ảnh.
            //Nếu không chọn ảnh mới thì giữ ảnh cũ.
            if (model.AnhDaiDienFile != null && model.AnhDaiDienFile.Length > 0)
            {
                try
                {
                    string anhMoi = await LuuAnhDaiDienAsync(model.AnhDaiDienFile);
                    user.AnhDaiDien = anhMoi;
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("AnhDaiDienFile", ex.Message);
                    model.AnhDaiDien = anhCu;
                    model.MatKhau = "";
                    return View(model);
                }
            }
            else
            {
                user.AnhDaiDien = anhCu;
            }
            //Cập nhật tên trong đơn đặt tour
            var dsDatTour = _context.DatTours
                .Where(x => x.TenKhach == tenCu)
                .ToList();

            foreach (var item in dsDatTour)
            {
                item.TenKhach = user.HoTen;
            }

            _context.SaveChanges();

            LuuSessionDangNhap(user);

            TempData["ThanhCong"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Index", "TrangChu");
        }
    }
}