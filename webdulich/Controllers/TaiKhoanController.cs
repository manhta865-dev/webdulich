using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace webdulich.Controllers
{
    public class TaiKhoanController : Controller
    {
        private readonly DuLichDbContext _context;
        private readonly webdulich.Services.GioiHanDangNhap _gioiHan;

        public TaiKhoanController(DuLichDbContext context, webdulich.Services.GioiHanDangNhap gioiHan)
        {
            _context = context;
            _gioiHan = gioiHan;
        }

        private static string CauBaoKhoa(TimeSpan conLai) =>
            $"Bạn đã nhập sai {webdulich.Services.GioiHanDangNhap.SoLanToiDa} lần. Vui lòng thử lại sau {Math.Ceiling(conLai.TotalMinutes)} phút.";
        // Hàm này dùng để lưu ảnh đại diện của người dùng.
        private async Task<string> LuuAnhDaiDienAsync(IFormFile file)
        {
            // TẠO THƯ MỤC ẢNH 
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "avatar"); // ĐƯỜNG DẪN

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Kiểm tra nội dung tệp thật, đuôi tệp lưu xuống cũng lấy từ nội dung, không tin tên tệp người dùng gửi
            if (!webdulich.Services.KiemTraTepAnh.HopLe(file, 5, out var extension, out var loiAnh))
            {
                throw new InvalidOperationException(loiAnh);
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
        public IActionResult DangKy(string? returnUrl)
        {
            ViewBag.ReturnUrl = TrangQuayLai(returnUrl);
            return View();
        }

        // Trang cần quay lại sau đăng nhập (ví dụ link mời đơn nhóm). Chỉ nhận đường dẫn nội bộ
        // để link không bị lợi dụng chuyển người dùng sang trang web khác.
        private string? TrangQuayLai(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

        // Dùng khi người dùng bấm nút Đăng ký.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangKy(TaiKhoan tk, string? returnUrl)
        {
            ViewBag.ReturnUrl = TrangQuayLai(returnUrl);
            // Nếu chưa nhập họ tên thì báo lỗi.
            if (string.IsNullOrWhiteSpace(tk.HoTen))
            {
                ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên!");
            }
            else if (tk.HoTen.Trim().Length > DinhDanhTaiKhoan.DoDaiHoTenToiDa)
            {
                ModelState.AddModelError("HoTen", DinhDanhTaiKhoan.LoiHoTenQuaDai);
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

                bool laEmail = DinhDanhTaiKhoan.LaEmail(taiKhoan);
                bool laSoDienThoai = DinhDanhTaiKhoan.LaSoDienThoai(taiKhoan);

                if (!laEmail && !laSoDienThoai)
                {
                    ModelState.AddModelError("TaiKhoanDangNhap", "Vui lòng nhập đúng email hoặc số điện thoại 10 số!");
                }
                else if (_context.TaiKhoans.DaDuocDung(laEmail ? taiKhoan : null, laSoDienThoai ? taiKhoan : null)) //Kiểm tra trùng tài khoản
                {
                    ModelState.AddModelError("TaiKhoanDangNhap", "Email hoặc số điện thoại này đã được đăng ký!");
                }
                else
                {
                    // Ô nhập chung được tách vào đúng cột Email hoặc SoDienThoai
                    tk.Email = laEmail ? taiKhoan : null;
                    tk.SoDienThoai = laSoDienThoai ? taiKhoan : null;
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
            // Mã hóa đúng mật khẩu người dùng gõ, không cắt dấu cách: lúc đăng nhập cũng so nguyên văn
            tk.MatKhau = BCrypt.Net.BCrypt.HashPassword(tk.MatKhau);
            tk.VaiTro = "user"; // Tài khoản tự đăng ký mặc định là user.
            tk.NgayTao = GioVN.BayGio;
            tk.AnhDaiDien = "";
            // Thêm tài khoản mới vào bảng TaiKhoans.
            _context.TaiKhoans.Add(tk);
            _context.SaveChanges();

            TempData["ThanhCong"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("DangNhap", new { returnUrl = TrangQuayLai(returnUrl) });
        }
        // Dùng để mở trang đăng nhập.
        [HttpGet]
        public IActionResult DangNhap(string? returnUrl, int? loiGoogle)
        {
            ViewBag.ReturnUrl = TrangQuayLai(returnUrl);
            if (loiGoogle == 1)
            {
                ViewBag.LoiGoogle = "Chưa đăng nhập được bằng Google. Nếu bạn mở từ Zalo hay Messenger, hãy mở link bằng Chrome hoặc Safari rồi thử lại, hoặc đăng nhập bằng mật khẩu.";
            }
            return View();
        }
        // Dùng khi người dùng bấm Đăng nhập.
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Tham số cho phép rỗng để ASP.NET không tự thêm lỗi tiếng Anh "The ... field is required"; tự kiểm bằng câu tiếng Việt
        public IActionResult DangNhap(string? taiKhoanDangNhap, string? matKhau, string? returnUrl)
        {
            ViewBag.ReturnUrl = TrangQuayLai(returnUrl);
            if (string.IsNullOrWhiteSpace(taiKhoanDangNhap) || string.IsNullOrWhiteSpace(matKhau))
            {
                if (string.IsNullOrWhiteSpace(taiKhoanDangNhap))
                {
                    ModelState.AddModelError("taiKhoanDangNhap", "Vui lòng nhập email hoặc số điện thoại!");
                }
                if (string.IsNullOrWhiteSpace(matKhau))
                {
                    ModelState.AddModelError("matKhau", "Vui lòng nhập mật khẩu!");
                }
                return View();
            }

            string taiKhoan = taiKhoanDangNhap.Trim();
            string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Đang bị khóa thì không kiểm mật khẩu, kể cả mật khẩu đúng
            if (_gioiHan.ConKhoa(taiKhoan, ip) is TimeSpan conKhoa)
            {
                ModelState.AddModelError("", CauBaoKhoa(conKhoa));
                return View();
            }

            // Tìm tài khoản trong database theo email hoặc số điện thoại.
            var user = _context.TaiKhoans.TheoDinhDanh(taiKhoan).FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(matKhau, user.MatKhau)) // có 2 trường hơp lỗi BCrypt.Verify() dùng để so sánh mật khẩu người dùng nhập với mật khẩu đã mã hóa trong database.
            {
                var biKhoa = _gioiHan.GhiSai(taiKhoan, ip);
                ModelState.AddModelError("", biKhoa is TimeSpan t ? CauBaoKhoa(t) : "Tài khoản hoặc mật khẩu không đúng!");
                return View();
            }
            _gioiHan.XoaDem(taiKhoan, ip);
            // Lưu session đăng nhập
            LuuSessionDangNhap(user);
            // điều hướng theo vai trò 
            if (user.VaiTro == "admin")
            {
                return RedirectToAction("TongQuan", "Admin");
            }

            if (user.VaiTro == "huongdanvien" || user.VaiTro == "taixe")
            {
                return RedirectToAction("LichLamViec", "NhanVien");
            }

            // Khách đăng nhập từ link mời (hoặc trang khác) thì quay lại đúng trang đó
            if (ViewBag.ReturnUrl is string quayLai)
            {
                return Redirect(quayLai);
            }

            return RedirectToAction("Index", "TrangChu");
        }
        // ---------- Đăng nhập Google ----------
        public const string CookieTamGoogle = "GoogleTam";

        // Không có khóa thì Program.cs không đăng ký Google; nút đã ẩn, chặn thêm khi gọi thẳng địa chỉ
        private bool CoGoogle()
        {
            var cauHinh = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            return !string.IsNullOrEmpty(cauHinh["Google:ClientId"]) && !string.IsNullOrEmpty(cauHinh["Google:ClientSecret"]);
        }

        // Bắt đầu bằng POST có antiforgery để trang khác không tự kích hoạt đăng nhập được
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DangNhapGoogle(string? returnUrl)
        {
            if (!CoGoogle())
            {
                return RedirectToAction("DangNhap", new { returnUrl = TrangQuayLai(returnUrl) });
            }
            var quayVe = Url.Action("GoogleXong", new { returnUrl = TrangQuayLai(returnUrl) });
            // Máy dùng chung: luôn hỏi chọn tài khoản Google
            return Challenge(new GoogleChallengeProperties { RedirectUri = quayVe, Prompt = "select_account" }, "Google");
        }

        // Google xác thực xong: tìm hoặc tạo tài khoản theo email đã xác minh rồi ghi Session như đăng nhập thường
        [HttpGet]
        public async Task<IActionResult> GoogleXong(string? returnUrl)
        {
            if (!CoGoogle())
            {
                return RedirectToAction("DangNhap", new { returnUrl = TrangQuayLai(returnUrl) });
            }
            var ketQua = await HttpContext.AuthenticateAsync(CookieTamGoogle);
            await HttpContext.SignOutAsync(CookieTamGoogle);

            var email = ketQua.Principal?.FindFirstValue(ClaimTypes.Email)?.Trim();
            bool daXacMinh = string.Equals(ketQua.Principal?.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase);
            if (!ketQua.Succeeded || string.IsNullOrEmpty(email) || !daXacMinh)
            {
                TempData["LoiGoogle"] = ketQua.Succeeded
                    ? "Không đăng nhập được bằng Google: tài khoản chưa có email đã xác minh."
                    : "Chưa đăng nhập được bằng Google, bạn thử lại nhé.";
                return RedirectToAction("DangNhap", new { returnUrl = TrangQuayLai(returnUrl) });
            }

            var user = _context.TaiKhoans.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                // Mật khẩu ngẫu nhiên không ai biết: tài khoản này chỉ vào được bằng Google
                // Tên Google dài quá 100 ký tự thì cắt bớt (người dùng sửa lại được ở Hồ sơ)
                var tenGoogle = (ketQua.Principal?.FindFirstValue(ClaimTypes.Name) ?? email).Trim();
                if (tenGoogle.Length > DinhDanhTaiKhoan.DoDaiHoTenToiDa)
                {
                    tenGoogle = tenGoogle[..DinhDanhTaiKhoan.DoDaiHoTenToiDa].TrimEnd();
                }
                user = new TaiKhoan
                {
                    HoTen = tenGoogle,
                    Email = email,
                    VaiTro = "user",
                    AnhDaiDien = "",
                    MatKhau = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)))
                };
                _context.TaiKhoans.Add(user);
                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    // Hai lần bấm cùng lúc: chỉ mục duy nhất của Email chặn lần sau, dùng tài khoản lần đầu vừa tạo
                    _context.Entry(user).State = EntityState.Detached;
                    user = _context.TaiKhoans.First(x => x.Email == email);
                }
            }

            // Admin và nhân viên vẫn phải dùng mật khẩu
            if (user.VaiTro != "user")
            {
                TempData["LoiGoogle"] = "Tài khoản quản trị và nhân viên đăng nhập bằng mật khẩu.";
                return RedirectToAction("DangNhap");
            }

            LuuSessionDangNhap(user);
            if (TrangQuayLai(returnUrl) is string quayLai)
            {
                return Redirect(quayLai);
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
            var user = _context.TaiKhoans.FirstOrDefault(x => x.Id == taiKhoanId.Value);

            if (user == null)
            {
                return RedirectToAction("DangNhap");
            }

            return View(TaoHoSo(user));
        }

        // Đưa tài khoản sang dữ liệu của trang hồ sơ (không kèm mật khẩu)
        private static HoSoViewModel TaoHoSo(TaiKhoan user) => new HoSoViewModel
        {
            HoTen = user.HoTen,
            Email = user.Email,
            SoDienThoai = user.SoDienThoai,
            AnhDaiDien = user.AnhDaiDien,
            VaiTro = user.VaiTro,
            NgayTao = user.NgayTao
        };
        // Dùng khi người dùng bấm Cập nhật hồ sơ.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaHoSo(HoSoViewModel model)
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

            string? email = DinhDanhTaiKhoan.ChuanHoa(model.Email);
            string? soDienThoai = DinhDanhTaiKhoan.ChuanHoa(model.SoDienThoai);

            // Email và số điện thoại: cần ít nhất một, đúng định dạng, chưa thuộc tài khoản khác
            if (email == null && soDienThoai == null)
            {
                ModelState.AddModelError("Email", "Cần giữ ít nhất email hoặc số điện thoại để đăng nhập.");
            }
            if (email != null && !DinhDanhTaiKhoan.LaEmail(email))
            {
                ModelState.AddModelError("Email", "Email chưa đúng định dạng, ví dụ ten@gmail.com.");
            }
            if (soDienThoai != null && !DinhDanhTaiKhoan.LaSoDienThoai(soDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.");
            }
            if (email != null && _context.TaiKhoans.DaDuocDung(email, null, user.Id))
            {
                ModelState.AddModelError("Email", "Email này đã thuộc một tài khoản khác.");
            }
            if (soDienThoai != null && _context.TaiKhoans.DaDuocDung(null, soDienThoai, user.Id))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại này đã thuộc một tài khoản khác.");
            }

            // Đổi mật khẩu: chỉ khi có nhập mật khẩu mới, và phải đúng mật khẩu hiện tại
            bool doiMatKhau = !string.IsNullOrWhiteSpace(model.MatKhauMoi);
            if (doiMatKhau)
            {
                if (string.IsNullOrWhiteSpace(model.MatKhauHienTai) || !BCrypt.Net.BCrypt.Verify(model.MatKhauHienTai, user.MatKhau))
                {
                    ModelState.AddModelError("MatKhauHienTai", "Mật khẩu hiện tại chưa đúng.");
                }
                if (model.MatKhauMoi!.Length < 6)
                {
                    ModelState.AddModelError("MatKhauMoi", "Mật khẩu mới cần ít nhất 6 ký tự.");
                }
                if (model.MatKhauMoi != model.XacNhanMatKhau)
                {
                    ModelState.AddModelError("XacNhanMatKhau", "Hai lần nhập mật khẩu mới chưa khớp.");
                }
            }

            // Ảnh đại diện: chỉ lưu file khi mọi thông tin khác đã hợp lệ
            string? anhMoi = null;
            if (ModelState.IsValid && model.AnhDaiDienFile != null && model.AnhDaiDienFile.Length > 0)
            {
                try
                {
                    anhMoi = await LuuAnhDaiDienAsync(model.AnhDaiDienFile);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("AnhDaiDienFile", ex.Message);
                }
            }

            if (!ModelState.IsValid)
            {
                // Trả lại form với thông tin đang nhập, không gửi lại mật khẩu
                model.AnhDaiDien = user.AnhDaiDien;
                model.VaiTro = user.VaiTro;
                model.NgayTao = user.NgayTao;
                model.MatKhauHienTai = model.MatKhauMoi = model.XacNhanMatKhau = null;
                return View(model);
            }

            user.HoTen = model.HoTen.Trim();
            user.Email = email;
            user.SoDienThoai = soDienThoai;
            if (doiMatKhau)
            {
                // Giữ nguyên văn như lúc đăng ký (không cắt dấu cách) để đăng nhập so khớp được
                user.MatKhau = BCrypt.Net.BCrypt.HashPassword(model.MatKhauMoi!);
            }
            if (anhMoi != null)
            {
                user.AnhDaiDien = anhMoi;
            }

            // Không còn đổi TenKhach của các đơn theo họ tên cũ: tên trên đơn là thông tin
            // liên hệ của riêng đơn đó, và so theo họ tên có thể đổi nhầm đơn của người trùng tên.
            _context.SaveChanges();

            LuuSessionDangNhap(user);

            TempData["ThanhCong"] = doiMatKhau ? "Đã lưu hồ sơ và đổi mật khẩu." : "Đã lưu hồ sơ.";
            return RedirectToAction("SuaHoSo");
        }
    }
}