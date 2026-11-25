using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    // Form admin chính sửa tài khoản (RB-51). Mật khẩu chỉ đổi khi nhập mật khẩu mới.
    // Vai trò chỉ đổi giữa khách hàng và quản trị viên; hướng dẫn viên, tài xế đổi ở mục Nhân sự (gắn với hồ sơ NhanViens).
    public class SuaTaiKhoanViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string HoTen { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? SoDienThoai { get; set; }

        public string? MatKhauMoi { get; set; }

        public string VaiTro { get; set; } = "user";

        // Nhóm quyền được tick (chỉ có nghĩa khi vai trò là quản trị viên)
        public List<string> DsQuyen { get; set; } = new();

        // Máy chủ điền, không nhận từ form
        public bool LaChinhMinh { get; set; }
        public bool LaNhanVien { get; set; }
    }
}
