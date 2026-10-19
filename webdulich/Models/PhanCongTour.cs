using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class PhanCongTour
    {
        [Key]
        public int Id { get; set; }

        public int LichKhoiHanhTourId { get; set; }

        public int HuongDanVienId { get; set; }

        public int? TaiXeId { get; set; }

        public string? GhiChu { get; set; }

        public DateTime NgayPhanCong { get; set; } = DateTime.Now;

        [ForeignKey("LichKhoiHanhTourId")]
        public LichKhoiHanhTour? LichKhoiHanhTour { get; set; }

        [ForeignKey("HuongDanVienId")]
        public NhanVien? HuongDanVien { get; set; }

        [ForeignKey("TaiXeId")]
        public NhanVien? TaiXe { get; set; }
    }
}