using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class TinNhanLienHe
    {
        [Key]
        public int Id { get; set; }

        // Khách của cuộc trò chuyện (tin admin trả lời cũng gắn tài khoản khách); tên, email lấy qua đây
        public int TaiKhoanId { get; set; }

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