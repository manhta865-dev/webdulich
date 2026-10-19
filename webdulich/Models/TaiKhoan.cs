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

        [Required(ErrorMessage = "Vui lòng nhập email hoặc số điện thoại!")]
        public string TaiKhoanDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu!")]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò!")]
        public string VaiTro { get; set; } = "user";

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public string? AnhDaiDien { get; set; }

        [NotMapped]
        public IFormFile? AnhDaiDienFile { get; set; }
        public ICollection<DatTours> DatTours { get; set; } = new List<DatTours>();
    }
}