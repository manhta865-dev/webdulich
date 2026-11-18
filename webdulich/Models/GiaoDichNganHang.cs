using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Một khoản tiền vào tài khoản nhận tiền do dịch vụ đọc biến động số dư (SePay) báo về qua webhook (Mo22, RB-48).
    // Lưu mọi khoản nhận được, kể cả khoản không khớp đơn nào, để đối chiếu và xử lý tay khi có tranh chấp.
    // Phụ thuộc hàm: Id → mọi cột; (Nguon, MaGiaoDichNguon) → mọi cột (khóa ứng viên, chống nhận trùng một giao dịch).
    // Số tiền, nội dung, thời gian là sự thật ngân hàng báo về, không suy ra từ bảng khác (3NF).
    public class GiaoDichNganHang
    {
        [Key]
        public int Id { get; set; }

        // Dịch vụ báo về, hiện chỉ có "SePay"
        [MaxLength(20)]
        public string Nguon { get; set; } = NguonTienVao.SePay;

        // Mã giao dịch bên dịch vụ (SePay: trường id)
        [MaxLength(64)]
        public string MaGiaoDichNguon { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? NganHang { get; set; }

        [MaxLength(30)]
        public string? SoTaiKhoan { get; set; }

        // Thời điểm ngân hàng ghi nhận (theo giờ dịch vụ báo, giờ Việt Nam)
        public DateTime ThoiGianGiaoDich { get; set; }

        public decimal SoTien { get; set; }

        [MaxLength(500)]
        public string NoiDung { get; set; } = string.Empty;

        // Mã tham chiếu của ngân hàng (SePay: referenceCode)
        [MaxLength(100)]
        public string? MaThamChieu { get; set; }

        public DateTime NgayNhan { get; set; }

        // DaKhop, KhongTimThayMa, SaiSoTien, DaTraRoi, GheDaNha, DonDaHuy (CHECK); xem KetQuaTienVao
        [MaxLength(20)]
        public string KetQua { get; set; } = KetQuaTienVao.KhongTimThayMa;

        // Giao dịch thanh toán được xác nhận nhờ khoản này; chỉ có khi KetQua = DaKhop (CHECK)
        public int? GiaoDichThanhToanId { get; set; }

        // Admin đã xử lý tay khoản không khớp (hoàn tiền, xác nhận tay...): người và lúc xử lý, cùng có hoặc cùng rỗng (CHECK)
        public int? NguoiXuLyId { get; set; }

        public DateTime? NgayXuLy { get; set; }

        [MaxLength(300)]
        public string? GhiChuXuLy { get; set; }

        [ForeignKey("GiaoDichThanhToanId")]
        public GiaoDichThanhToan? GiaoDichThanhToan { get; set; }

        [ForeignKey("NguoiXuLyId")]
        public TaiKhoan? NguoiXuLy { get; set; }
    }

    public static class NguonTienVao
    {
        public const string SePay = "SePay";
        // Khoản tạo từ trang giả lập của admin (chỉ bật khi thử)
        public const string GiaLap = "GiaLap";
    }

    public static class KetQuaTienVao
    {
        public const string DaKhop = "DaKhop";
        public const string KhongTimThayMa = "KhongTimThayMa";
        public const string SaiSoTien = "SaiSoTien";
        public const string DaTraRoi = "DaTraRoi";
        public const string GheDaNha = "GheDaNha";
        public const string DonDaHuy = "DonDaHuy";

        public static string Ten(string ketQua) => ketQua switch
        {
            DaKhop => "Đã tự xác nhận",
            KhongTimThayMa => "Không tìm thấy mã đơn hoặc ghế",
            SaiSoTien => "Sai số tiền",
            DaTraRoi => "Ghế hoặc đơn đã trả trước đó",
            GheDaNha => "Ghế đã bị nhả",
            DonDaHuy => "Đơn đã hủy",
            _ => ketQua
        };
    }
}
