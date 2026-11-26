using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Đơn đặt tour (cá nhân hoặc nhóm). Tour, ngày đi lấy qua lịch; số người, tiền lấy từ ghế;
    // trạng thái thanh toán lấy từ giao dịch (docs/MO_HINH_DU_LIEU.md, mục 5).
    public class DatTours
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Thông tin liên hệ do người đặt nhập cho đơn này (độc lập với hồ sơ tài khoản)
        // Độ dài theo cột CSDL (Mo28): nvarchar(100), nvarchar(20), bắt buộc
        [Required(ErrorMessage = "Vui lòng nhập tên khách")]
        [StringLength(100, ErrorMessage = "Tên tối đa 100 ký tự")]
        public string TenKhach { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        public string DienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string DiaChi { get; set; } = string.Empty;

        public string? GhiChu { get; set; }

        public int? KhuyenMaiId { get; set; }

        // Snapshot số tiền giảm lúc đặt
        public decimal SoTienGiam { get; set; }

        [ForeignKey("KhuyenMaiId")]
        public KhuyenMai? KhuyenMai { get; set; }

        public string MaChuyenDi { get; set; } = string.Empty;

        public int LichKhoiHanhTourId { get; set; }

        [ForeignKey("LichKhoiHanhTourId")]
        public LichKhoiHanhTour? LichKhoiHanhTour { get; set; }

        // CaNhan hoặc Nhom
        public string LoaiDon { get; set; } = "CaNhan";

        // GiuCho, DaXacNhan, DaHuy
        public string TrangThai { get; set; } = "GiuCho";

        public string? MaMoi { get; set; }

        public DateTime? HanGiuCho { get; set; }

        // Đơn nhóm đã được gia hạn một lần khi hết hạn giữ chỗ (nhắc rồi mới nhả)
        public bool DaGiaHan { get; set; }

        public DateTime NgayDat { get; set; } = GioVN.BayGio;

        public int? TaiKhoanId { get; set; }

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }

        public ICollection<HanhKhachDatTour> HanhKhachDatTours { get; set; } = new List<HanhKhachDatTour>();

        // ---------- Chỉ dùng cho form đặt tour, không lưu (đã bỏ cột ở bước thu hẹp) ----------
        [NotMapped]
        public int TourId { get; set; }

        [NotMapped]
        [Range(1, 100, ErrorMessage = "Số người phải từ 1 trở lên")]
        public int SoNguoi { get; set; }

        [NotMapped]
        public DateTime NgayDi { get; set; }
    }
}
