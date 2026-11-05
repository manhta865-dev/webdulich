using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class LichKhoiHanhTour
    {
        [Key]
        public int Id { get; set; }

        public int TourId { get; set; }

        public DateTime NgayKhoiHanh { get; set; }

        public TimeSpan? GioKhoiHanh { get; set; }

        public int SoChoCon { get; set; }

        // Cột mới (bước mở rộng): sức chứa và sơ đồ chỗ ngồi của chuyến
        public int SoChoToiDa { get; set; }

        public int? SoDoChoNgoiId { get; set; }

        [ForeignKey("SoDoChoNgoiId")]
        public SoDoChoNgoi? SoDoChoNgoi { get; set; }

        public decimal? GiaNguoiLon { get; set; }

        public decimal? GiaTreEm { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        [NotMapped]
        public decimal GiaNguoiLonHienThi => GiaNguoiLon ?? Tour?.Gia ?? 0;

        [NotMapped]
        public decimal GiaTreEmHienThi => GiaTreEm ?? GiaNguoiLonHienThi * 0.5m;
        // Chỉ lưu quyết định của admin: MoBan, TamNgung, DaHuy (xem TrangThaiLich).
        // "Sắp khởi hành", "Đang diễn ra", "Đã hoàn thành" tính từ ngày ở TrangThaiHienThi.
        public string TrangThai { get; set; } = TrangThaiLich.MoBan;
        [NotMapped]
        public string TrangThaiHienThi
        {
            get
            {
                if (TrangThai == TrangThaiLich.DaHuy)
                {
                    return "Đã hủy";
                }

                var homNay = GioVN.HomNay;
                var ngayBatDau = NgayKhoiHanh.Date;
                var soNgay = Tour?.SoNgay ?? 1;

                if (soNgay <= 0)
                {
                    soNgay = 1;
                }

                var ngayKetThuc = ngayBatDau.AddDays(soNgay - 1);

                if (homNay < ngayBatDau)
                {
                    return "Sắp khởi hành";
                }

                if (homNay >= ngayBatDau && homNay <= ngayKetThuc)
                {
                    return "Đang diễn ra";
                }

                return "Đã hoàn thành";
            }
        }

        // Admin tạm ngưng nhận đặt (lịch vẫn diễn ra, đơn cũ giữ nguyên)
        [NotMapped]
        public bool DangTamNgung => TrangThai == TrangThaiLich.TamNgung;

        // Khách chỉ đặt được lịch đang mở bán và chưa tới ngày khởi hành (RB-19)
        [NotMapped]
        public bool ChoPhepDatTour => TrangThai == TrangThaiLich.MoBan && TrangThaiHienThi == "Sắp khởi hành";
    }

    public static class TrangThaiLich
    {
        public const string MoBan = "MoBan";
        public const string TamNgung = "TamNgung";
        public const string DaHuy = "DaHuy";
    }
}