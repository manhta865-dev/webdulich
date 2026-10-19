using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class TinNhanLienHe
    {
        [Key]
        public int Id { get; set; }

        public int? TaiKhoanId { get; set; }

        public string HoTen { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        public string NoiDung { get; set; } = string.Empty;

        public string NguoiGui { get; set; } = "user"; // user hoặc admin

        public DateTime NgayGui { get; set; } = DateTime.Now;

        public bool DaDoc { get; set; } = false;

        public DateTime? NgayDoc { get; set; }

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }
    }
}