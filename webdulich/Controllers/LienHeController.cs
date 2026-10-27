using Microsoft.AspNetCore.Mvc;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class LienHeController : Controller
    {
        private readonly DuLichDbContext _context;
        //
        public LienHeController(DuLichDbContext context)
        {
            _context = context;
        }
        // Phương thức này xử lý yêu cầu gửi tin nhắn liên hệ từ người dùng. Nó nhận vào nội dung tin nhắn và lưu thông tin liên hệ vào cơ sở dữ liệu, bao gồm tên người gửi, email, nội dung tin nhắn, thời gian gửi và trạng thái đã đọc. Kết quả trả về là một đối tượng JSON cho biết việc gửi tin nhắn có thành công hay không.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuiTinNhanAjax(string noiDung)
        {
            // Lấy thông tin người dùng từ session
            var user = HttpContext.Session.GetString("user");
            var taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");
            // Kiểm tra nếu người dùng chưa đăng nhập hoặc không có thông tin tài khoản
            if (string.IsNullOrEmpty(user) || !taiKhoanId.HasValue)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }
            // Kiểm tra nếu nội dung tin nhắn rỗng hoặc chỉ chứa khoảng trắng
            if (string.IsNullOrWhiteSpace(noiDung))
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung tin nhắn." });
            }
            // Tạo đối tượng tin nhắn mới. Tên và email người gửi lấy qua TaiKhoanId khi hiển thị,
            // HoTen và Email của tin nhắn để trống (sẽ bỏ cột ở bước thu hẹp).
            var tinNhan = new TinNhanLienHe
            {
                TaiKhoanId = taiKhoanId.Value,
                HoTen = string.Empty,
                Email = string.Empty,
                NoiDung = noiDung.Trim(),
                NguoiGui = "user",
                NgayGui = DateTime.Now,
                DaDoc = false
            };
            // Lưu xuống SQL Server.
            _context.TinNhanLienHes.Add(tinNhan);
            _context.SaveChanges();
            // Trả kết quả
            return Json(new { success = true });
        }
        // Tải danh sách tin nhắn
        [HttpGet]
        public IActionResult LayTinNhan()
        {
            // Lấy tài khoản hiện tại
            var taiKhoanId = HttpContext.Session.GetInt32("taiKhoanId");
            // 
            if (!taiKhoanId.HasValue)
            {
                return Json(new List<object>());
            }

            var ds = _context.TinNhanLienHes
                .Where(x => x.TaiKhoanId == taiKhoanId.Value) // Chỉ lấy của người đang đăng nhập
                .OrderBy(x => x.NgayGui)
                .Select(x => new
                {
                    x.Id,
                    x.NoiDung,
                    x.NguoiGui,
                    ThoiGian = x.NgayGui.ToString("dd/MM HH:mm")
                })
                .ToList();

            return Json(ds);
        }
    }
}