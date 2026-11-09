using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Ghế: suất đi của một người trong một đơn. Thông tin cá nhân có thể trống cho đến khi
    // thành viên tự điền (đơn nhóm) hoặc admin thêm người vào đơn.
    public class HanhKhachDatTour
    {
        [Key]
        public int Id { get; set; }

        public int DatTourId { get; set; }

        public string? HoTen { get; set; }

        public string? LoaiGiayTo { get; set; }

        public string? SoGiayTo { get; set; }

        // "Người lớn" hoặc "Trẻ nhỏ"
        [Required(ErrorMessage = "Vui lòng chọn loại hành khách")]
        public string LoaiHanhKhach { get; set; } = "Người lớn";

        // Tài khoản đã nhận ghế (đơn nhóm)
        public int? TaiKhoanId { get; set; }

        // Snapshot giá lúc đặt
        public decimal GiaLucDat { get; set; }

        // Phần tiền giảm của ghế này lúc đặt (snapshot, RB-40): đơn nhóm dùng mã khuyến mãi thì tiền giảm
        // chia cho từng ghế theo tỷ lệ giá. Đơn cá nhân luôn 0 (giảm lưu ở DatTours.SoTienGiam).
        public decimal TienGiam { get; set; }

        // Số tiền ghế này phải trả
        [NotMapped]
        public decimal SoTienPhaiTra => GiaLucDat - TienGiam;

        // Nội dung chuyển khoản riêng của ghế
        public string? MaThanhToan { get; set; }

        // Có giá trị nghĩa là ghế đã nhả
        public DateTime? NgayNha { get; set; }

        [ForeignKey("DatTourId")]
        public DatTours? DatTour { get; set; }

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }
    }
}
