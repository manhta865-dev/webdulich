using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    // Form admin sửa tài khoản. Mật khẩu chỉ đổi khi admin nhập mật khẩu mới.
    public class SuaTaiKhoanViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string HoTen { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? SoDienThoai { get; set; }

        public string? MatKhauMoi { get; set; }

        public string VaiTro { get; set; } = "user";
    }
}
