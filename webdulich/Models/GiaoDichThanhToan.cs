using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Một lần trả tiền, gắn với cả đơn (đơn cá nhân) hoặc với một ghế (đơn nhóm), không gắn cả hai
    public class GiaoDichThanhToan
    {
        [Key]
        public int Id { get; set; }

        public int? DatTourId { get; set; }

        public int? HanhKhachDatTourId { get; set; }

        public int? NguoiXacNhanId { get; set; }

        // Ai đã trả (chủ ghế, hoặc trưởng nhóm trả hộ); rỗng với giao dịch cũ hoặc tiền mặt tại quầy
        public int? NguoiTraId { get; set; }

        public decimal SoTien { get; set; }

        public string PhuongThuc { get; set; } = string.Empty;

        // ChoXacNhan, ThanhCong, ThatBai
        public string TrangThai { get; set; } = "ChoXacNhan";

        public string? MaThamChieuNganHang { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayXacNhan { get; set; }

        [ForeignKey("DatTourId")]
        public DatTours? DatTour { get; set; }

        [ForeignKey("HanhKhachDatTourId")]
        public HanhKhachDatTour? HanhKhachDatTour { get; set; }

        [ForeignKey("NguoiXacNhanId")]
        public TaiKhoan? NguoiXacNhan { get; set; }

        [ForeignKey("NguoiTraId")]
        public TaiKhoan? NguoiTra { get; set; }
    }
}
