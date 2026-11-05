using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class NhanVien
    {
        [Key]
        public int Id { get; set; }

        public int TaiKhoanId { get; set; }

        // Họ tên, số điện thoại, vai trò (hướng dẫn viên, tài xế) lấy từ TaiKhoan

        public bool DangLamViec { get; set; } = true;

        public string? GhiChu { get; set; }

        public DateTime NgayTao { get; set; } = GioVN.BayGio;

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }
    }
}