using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Một lần nhắc qua email cho một người của đơn nhóm khi sắp hết hạn giữ chỗ mà còn ghế chưa trả (Mo21, RB-47).
    // Mỗi người được nhắc một lần cho mỗi mốc hạn: UNIQUE (DatTourId, TaiKhoanId, HanDuocNhac). Gia hạn đổi mốc hạn
    // nên được nhắc thêm một lần. Người nhận là trưởng nhóm hay thành viên suy ra từ DatTours.TaiKhoanId, không lưu.
    // Phụ thuộc hàm: Id → mọi cột; (DatTourId, TaiKhoanId, HanDuocNhac) → mọi cột (khóa ứng viên).
    // HanDuocNhac và EmailNhan là sự thật tại lúc nhắc (hạn có thể đổi khi gia hạn, email có thể đổi sau), không suy ra được về sau.
    public class NhacThanhToan
    {
        [Key]
        public int Id { get; set; }

        public int DatTourId { get; set; }

        // Người nhận mail
        public int TaiKhoanId { get; set; }

        // Mốc hạn giữ chỗ của đơn tại lúc nhắc
        public DateTime HanDuocNhac { get; set; }

        [MaxLength(256)]
        public string EmailNhan { get; set; } = string.Empty;

        // Lúc tạo lần nhắc (giành chỗ trước khi gửi)
        public DateTime NgayTao { get; set; }

        // Lúc gửi được; rỗng khi chưa gửi được (CHECK)
        public DateTime? NgayGui { get; set; }

        // ChoGui, DaGui, Loi (CHECK)
        [MaxLength(10)]
        public string TrangThai { get; set; } = TrangThaiNhac.ChoGui;

        // Số lần đã thử gửi, tối đa SoLanThuToiDa
        public int SoLanThu { get; set; } = 1;

        // Lý do lỗi của lần thử gần nhất
        [MaxLength(500)]
        public string? Loi { get; set; }

        [ForeignKey("DatTourId")]
        public DatTours? DatTour { get; set; }

        [ForeignKey("TaiKhoanId")]
        public TaiKhoan? TaiKhoan { get; set; }
    }

    public static class TrangThaiNhac
    {
        public const string ChoGui = "ChoGui";
        public const string DaGui = "DaGui";
        public const string Loi = "Loi";

        public const int SoLanThuToiDa = 3;
    }
}
