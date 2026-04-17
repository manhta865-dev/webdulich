using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class TaiKhoan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string HoTen { get; set; } = string.Empty;

        [Required]
        public string TaiKhoanDangNhap { get; set; } = string.Empty;

        [Required]
        public string MatKhau { get; set; } = string.Empty;

        public string VaiTro { get; set; } = "user";
    }
}