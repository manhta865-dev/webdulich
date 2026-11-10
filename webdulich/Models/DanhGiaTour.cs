using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Đánh giá của một khách cho một chuyến đi (một lịch khởi hành) đã đi xong (RB-43).
    // Gắn với lịch chứ không với tour: tour suy ra qua LichKhoiHanhTours.TourId nên không lưu lại (3NF).
    // Điểm trung bình, số lượt đánh giá của tour là dữ liệu dẫn xuất, tính bằng truy vấn, không lưu.
    // Mỗi tài khoản đánh giá một lịch tối đa một lần (UNIQUE TaiKhoanId, LichKhoiHanhTourId), sửa lại được.
    public class DanhGiaTour
    {
        [Key]
        public int Id { get; set; }

        public int LichKhoiHanhTourId { get; set; }

        public int TaiKhoanId { get; set; }

        // 1 đến 5 sao (CHECK)
        public int SoSao { get; set; }

        [MaxLength(1000)]
        public string? NoiDung { get; set; }

        public DateTime NgayTao { get; set; }

        public DateTime? NgaySua { get; set; }

        // Admin ẩn đánh giá vi phạm (lời lẽ xúc phạm, quảng cáo) thay vì xóa
        public bool DangHien { get; set; } = true;

        [ForeignKey("LichKhoiHanhTourId")]
        public LichKhoiHanhTour? LichKhoiHanhTour { get; set; }

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }
    }

    // Form viết hoặc sửa đánh giá của khách
    public class DanhGiaForm
    {
        public int LichId { get; set; }

        [Range(1, 5, ErrorMessage = "Chọn số sao từ 1 đến 5.")]
        public int SoSao { get; set; }

        [StringLength(1000, ErrorMessage = "Nhận xét tối đa 1000 ký tự.")]
        public string? NoiDung { get; set; }
    }

    // Một đánh giá hiện ở trang chi tiết tour
    public class DanhGiaHienThi
    {
        public string TenNguoi { get; set; } = string.Empty;
        public int SoSao { get; set; }
        public string? NoiDung { get; set; }
        public DateTime NgayDi { get; set; }
        public DateTime NgayViet { get; set; }
    }
}
