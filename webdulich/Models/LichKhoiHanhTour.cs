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
        public string TrangThai { get; set; } = "Sắp khởi hành";
        [NotMapped]
        public string TrangThaiHienThi
        {
            get
            {
                if (TrangThai == "Đã hủy")
                {
                    return "Đã hủy";
                }

                var homNay = DateTime.Today;
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

        [NotMapped]
        public bool ChoPhepDatTour => TrangThaiHienThi == "Sắp khởi hành";
    }
}