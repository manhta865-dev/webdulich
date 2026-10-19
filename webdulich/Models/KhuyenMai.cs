using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class KhuyenMai
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string TenKhuyenMai { get; set; } = string.Empty;

        [Required]
        public string MaKhuyenMai { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        // Tien: giảm số tiền cố định, PhanTram: giảm theo phần trăm.
        [Required]
        public string KieuGiamGia { get; set; } = "Tien";

        public decimal GiaTriGiam { get; set; }

        public DateTime NgayBatDau { get; set; } = DateTime.Today;

        public DateTime NgayKetThuc { get; set; } = DateTime.Today.AddMonths(1);

        public string MauNen { get; set; } = "#ff6b4a";

        public int ThuTu { get; set; } = 1;

        // DangHienThi hoặc TamAn.
        public string TrangThai { get; set; } = "DangHienThi";

        public int? SoLuotDungToiDa { get; set; }

        public int SoLuotDaDung { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public ICollection<DatTours> DatTours { get; set; } = new List<DatTours>();

        [NotMapped]
        public bool DangHoatDong
        {
            get
            {
                bool conHan = NgayBatDau.Date <= DateTime.Today && NgayKetThuc.Date >= DateTime.Today;
                bool conLuot = !SoLuotDungToiDa.HasValue || SoLuotDaDung < SoLuotDungToiDa.Value;

                return TrangThai == "DangHienThi" && conHan && conLuot;
            }
        }

        [NotMapped]
        public string GiaTriHienThi =>
            KieuGiamGia == "PhanTram"
                ? $"{GiaTriGiam:0.#}%"
                : $"{GiaTriGiam:N0} VNĐ";

        [NotMapped]
        public string TrangThaiHienThi =>
            DangHoatDong ? "Đang áp dụng" : TrangThai == "TamAn" ? "Tạm ẩn" : "Hết hiệu lực";
    }
}
