using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class HanhKhachDatTour
    {
        [Key]
        public int Id { get; set; }

        public int DatTourId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên hành khách")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại giấy tờ")]
        public string LoaiGiayTo { get; set; } = "CCCD";

        [Required(ErrorMessage = "Vui lòng nhập số giấy tờ")]
        public string SoGiayTo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại hành khách")]
        public string LoaiHanhKhach { get; set; } = "Người lớn";

        // Cột mới của mô hình ghế (bước mở rộng, chưa dùng trong code cũ)
        public int? TaiKhoanId { get; set; }

        public decimal GiaLucDat { get; set; }

        public string? MaThanhToan { get; set; }

        public DateTime? NgayNha { get; set; }

        [ForeignKey("DatTourId")]
        public DatTours? DatTour { get; set; }

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }
    }
}