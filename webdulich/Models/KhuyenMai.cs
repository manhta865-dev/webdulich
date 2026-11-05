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

        public DateTime NgayBatDau { get; set; } = GioVN.HomNay;

        public DateTime NgayKetThuc { get; set; } = GioVN.HomNay.AddMonths(1);

        public string MauNen { get; set; } = "#ff6b4a";

        public int ThuTu { get; set; } = 1;

        // HoatDong hoặc TamNgung (xem TrangThaiKhuyenMai).
        public string TrangThai { get; set; } = TrangThaiKhuyenMai.HoatDong;

        public int? SoLuotDungToiDa { get; set; }

        // Số đơn chưa hủy đang dùng mã này; nơi truy vấn điền vào để hiển thị
        [NotMapped]
        public int SoLuotDaDungThucTe { get; set; }

        public DateTime NgayTao { get; set; } = GioVN.BayGio;

        public ICollection<DatTours> DatTours { get; set; } = new List<DatTours>();

        [NotMapped]
        public bool DangHoatDong
        {
            get
            {
                bool conHan = NgayBatDau.Date <= GioVN.HomNay && NgayKetThuc.Date >= GioVN.HomNay;
                bool conLuot = !SoLuotDungToiDa.HasValue || SoLuotDaDungThucTe < SoLuotDungToiDa.Value;

                return TrangThai == TrangThaiKhuyenMai.HoatDong && conHan && conLuot;
            }
        }

        [NotMapped]
        public string GiaTriHienThi =>
            KieuGiamGia == "PhanTram"
                ? $"{GiaTriGiam:0.#}%"
                : $"{GiaTriGiam:N0} VNĐ";

        [NotMapped]
        public string TrangThaiHienThi =>
            DangHoatDong ? "Đang áp dụng" : TrangThai == TrangThaiKhuyenMai.TamNgung ? "Tạm ngưng" : "Hết hiệu lực";
    }

    public static class TrangThaiKhuyenMai
    {
        public const string HoatDong = "HoatDong";
        public const string TamNgung = "TamNgung";
    }
}
