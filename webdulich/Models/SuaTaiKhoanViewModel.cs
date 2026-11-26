using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    // Form admin chính sửa tài khoản (RB-51). Mật khẩu chỉ đổi khi nhập mật khẩu mới.
    // Vai trò: khách hàng và quản trị viên đổi qua lại với nhau; nhân viên (gắn hồ sơ NhanViens) chỉ đổi qua lại
    // hướng dẫn viên và tài xế, khi không còn chuyến chưa đi xong ở vai trò cũ. Nhân viên mới thêm ở mục Nhân sự.
    public class SuaTaiKhoanViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(DinhDanhTaiKhoan.DoDaiHoTenToiDa, ErrorMessage = DinhDanhTaiKhoan.LoiHoTenQuaDai)]
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
