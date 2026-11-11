using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Mức lương của một nhân viên từ một ngày trở đi (Mo20, RB-46). Đổi mức thì thêm dòng mới để tháng cũ vẫn đúng mức cũ.
    // Phụ thuộc hàm: Id → mọi cột; (NhanVienId, ApDungTu) là khóa ứng viên (UNIQUE).
    public class MucLuongNhanVien
    {
        [Key]
        public int Id { get; set; }

        public int NhanVienId { get; set; }

        // Lương cứng mỗi tháng
        public decimal LuongCoBan { get; set; }

        // Tiền công cho mỗi ngày đi tour được phân công
        public decimal TienCongNgay { get; set; }

        [Column(TypeName = "date")]
        public DateTime ApDungTu { get; set; }

        [MaxLength(300)]
        public string? GhiChu { get; set; }

        public DateTime NgayTao { get; set; }

        [ForeignKey("NhanVienId")]
        public NhanVien? NhanVien { get; set; }
    }

    // Thưởng (số dương) hoặc phạt (số âm) admin nhập tay cho một tháng lương. Tháng đã chốt thì không thêm, xóa nữa.
    public class KhoanLuong
    {
        [Key]
        public int Id { get; set; }

        public int NhanVienId { get; set; }

        public int Nam { get; set; }

        public int Thang { get; set; }

        public decimal SoTien { get; set; }

        [MaxLength(300)]
        public string NoiDung { get; set; } = string.Empty;

        public int NguoiTaoId { get; set; }

        public DateTime NgayTao { get; set; }

        [ForeignKey("NhanVienId")]
        public NhanVien? NhanVien { get; set; }

        [ForeignKey("NguoiTaoId")]
        public TaiKhoan? NguoiTao { get; set; }
    }

    // Bảng lương đã chốt của một nhân viên trong một tháng. Chỉ tạo khi admin chốt.
    // Các cột số liệu là bản chụp lúc chốt (snapshot hợp lệ): sau đó sửa phân công, đơn nghỉ hay mức lương
    // thì lương đã chốt không đổi. Tổng tiền không lưu vì tính được từ các cột này và KhoanLuongs của tháng.
    public class BangLuong
    {
        [Key]
        public int Id { get; set; }

        public int NhanVienId { get; set; }

        public int Nam { get; set; }

        public int Thang { get; set; }

        public decimal LuongCoBan { get; set; }

        public decimal TienCongNgay { get; set; }

        public int SoNgayDiTour { get; set; }

        public int SoNgayNghiTruLuong { get; set; }

        // Số ngày công chuẩn dùng để tính tiền trừ một ngày nghỉ lúc chốt
        public int SoNgayCongChuan { get; set; }

        public int NguoiChotId { get; set; }

        public DateTime NgayChot { get; set; }

        public int? NguoiTraId { get; set; }

        public DateTime? NgayTra { get; set; }

        [ForeignKey("NhanVienId")]
        public NhanVien? NhanVien { get; set; }

        [ForeignKey("NguoiChotId")]
        public TaiKhoan? NguoiChot { get; set; }

        [ForeignKey("NguoiTraId")]
        public TaiKhoan? NguoiTra { get; set; }
    }

    // Form đặt mức lương mới
    public class MucLuongForm
    {
        public int NhanVienId { get; set; }

        [Required(ErrorMessage = "Nhập lương cơ bản.")]
        [Range(0, 1_000_000_000, ErrorMessage = "Lương cơ bản từ 0 đến 1 tỷ đồng.")]
        public decimal? LuongCoBan { get; set; }

        [Required(ErrorMessage = "Nhập tiền công mỗi ngày đi tour.")]
        [Range(0, 100_000_000, ErrorMessage = "Tiền công ngày từ 0 đến 100 triệu đồng.")]
        public decimal? TienCongNgay { get; set; }

        [Required(ErrorMessage = "Chọn ngày bắt đầu áp dụng.")]
        [DataType(DataType.Date)]
        public DateTime? ApDungTu { get; set; }

        [StringLength(300, ErrorMessage = "Ghi chú tối đa 300 ký tự.")]
        public string? GhiChu { get; set; }
    }
}
