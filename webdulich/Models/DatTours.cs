using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class DatTours
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách")]
        public string TenKhach { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string DienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string DiaChi { get; set; } = string.Empty;

        public string? GhiChu { get; set; }

        [Range(1, 100, ErrorMessage = "Số người phải từ 1 trở lên")]
        public int SoNguoi { get; set; }

        public DateTime NgayDi { get; set; }

        public int TourId { get; set; }

        public decimal TongTien { get; set; }
        public int? KhuyenMaiId { get; set; }

        public string? MaKhuyenMai { get; set; }

        public decimal TongTienTruocGiam { get; set; }

        public decimal SoTienGiam { get; set; }

        [ForeignKey("KhuyenMaiId")]
        public KhuyenMai? KhuyenMai { get; set; }
        public string PhuongThucThanhToan { get; set; } = "Chuyển khoản";

        public string TrangThaiThanhToan { get; set; } = "Chưa thanh toán";

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }
        public string MaChuyenDi { get; set; } = string.Empty;
        public int? LichKhoiHanhTourId { get; set; }

        [ForeignKey("LichKhoiHanhTourId")]
        public LichKhoiHanhTour? LichKhoiHanhTour { get; set; }
        public int? TaiKhoanId { get; set; }

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }
        public ICollection<HanhKhachDatTour> HanhKhachDatTours { get; set; } = new List<HanhKhachDatTour>();
    }
}