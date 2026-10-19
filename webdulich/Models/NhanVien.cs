using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class NhanVien
    {
        [Key]
        public int Id { get; set; }

        public int TaiKhoanId { get; set; }

        public string HoTen { get; set; } = string.Empty;

        public string SoDienThoai { get; set; } = string.Empty;

        public string LoaiNhanVien { get; set; } = string.Empty;
        // HuongDanVien hoặc TaiXe

        public bool DangLamViec { get; set; } = true;

        public string? GhiChu { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }
    }
}