using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace webdulich.Models
{
    // Dữ liệu của trang Sửa hồ sơ. Tách khỏi TaiKhoan để form không gửi được vai trò hay mật khẩu đã băm.
    public class HoSoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? SoDienThoai { get; set; }

        public IFormFile? AnhDaiDienFile { get; set; }

        public string? MatKhauHienTai { get; set; }

        public string? MatKhauMoi { get; set; }

        public string? XacNhanMatKhau { get; set; }

        // Chỉ để hiển thị
        public string? AnhDaiDien { get; set; }
        public string VaiTro { get; set; } = "user";
        public DateTime NgayTao { get; set; }
    }
}
