using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace webdulich.Models
{
    public class TaiKhoan
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên!")]
        public string HoTen { get; set; } = string.Empty;

        // Ô nhập chung "Email hoặc số điện thoại" của form đăng ký; không lưu (đã tách thành Email, SoDienThoai)
        [NotMapped]
        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại!")]
        public string TaiKhoanDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu!")]
        public string MatKhau { get; set; } = string.Empty;

        // Dùng để đăng nhập; cần ít nhất một trong hai, mỗi giá trị chỉ thuộc một tài khoản
        public string? Email { get; set; }

        public string? SoDienThoai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò!")]
        public string VaiTro { get; set; } = "user";

        public DateTime NgayTao { get; set; } = GioVN.BayGio;

        public string? AnhDaiDien { get; set; }

        [NotMapped]
        public IFormFile? AnhDaiDienFile { get; set; }
        public ICollection<DatTours> DatTours { get; set; } = new List<DatTours>();
    }
}