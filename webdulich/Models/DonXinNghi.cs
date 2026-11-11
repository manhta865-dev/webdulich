using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webdulich.Models
{
    // Đơn xin nghỉ của hướng dẫn viên, tài xế (Mo19, RB-45).
    // Khác NhanViens.DangLamViec (nghỉ hẳn, admin bật tắt): đơn này là nghỉ theo ngày, nhân viên gửi, admin duyệt.
    // Phụ thuộc hàm: Id → mọi cột. Người duyệt, ngày duyệt là sự kiện của lần duyệt, không suy ra từ cột khác (3NF).
    public class DonXinNghi
    {
        [Key]
        public int Id { get; set; }

        public int NhanVienId { get; set; }

        [Column(TypeName = "date")]
        public DateTime TuNgay { get; set; }

        [Column(TypeName = "date")]
        public DateTime DenNgay { get; set; }

        [MaxLength(500)]
        public string LyDo { get; set; } = string.Empty;

        // ChoDuyet, DaDuyet, TuChoi, DaRut (CHECK)
        [MaxLength(20)]
        public string TrangThai { get; set; } = TrangThaiDonNghi.ChoDuyet;

        public DateTime NgayGui { get; set; }

        // Admin duyệt hoặc từ chối; rỗng khi đơn còn chờ hoặc nhân viên tự rút (CHECK)
        public int? NguoiDuyetId { get; set; }

        public DateTime? NgayDuyet { get; set; }

        [MaxLength(300)]
        public string? GhiChuDuyet { get; set; }

        [ForeignKey("NhanVienId")]
        public NhanVien? NhanVien { get; set; }

        [ForeignKey("NguoiDuyetId")]
        public TaiKhoan? NguoiDuyet { get; set; }

        public int SoNgay => (DenNgay.Date - TuNgay.Date).Days + 1;
    }

    public static class TrangThaiDonNghi
    {
        public const string ChoDuyet = "ChoDuyet";
        public const string DaDuyet = "DaDuyet";
        public const string TuChoi = "TuChoi";
        public const string DaRut = "DaRut";

        public static string Ten(string t) => t switch
        {
            ChoDuyet => "Chờ duyệt",
            DaDuyet => "Đã duyệt",
            TuChoi => "Từ chối",
            DaRut => "Đã rút",
            _ => t
        };

        // Class nhãn trong admin.css: xanh (đã duyệt), vàng (chờ), xám (rút, từ chối)
        public static string Lop(string t) => t switch
        {
            DaDuyet => "",
            ChoDuyet => "canh-bao",
            _ => "tat"
        };
    }

    // Form gửi đơn của nhân viên
    public class DonXinNghiForm
    {
        [Required(ErrorMessage = "Chọn ngày bắt đầu nghỉ.")]
        [DataType(DataType.Date)]
        public DateTime? TuNgay { get; set; }

        [Required(ErrorMessage = "Chọn ngày kết thúc nghỉ.")]
        [DataType(DataType.Date)]
        public DateTime? DenNgay { get; set; }

        [Required(ErrorMessage = "Ghi lý do xin nghỉ.")]
        [StringLength(500, ErrorMessage = "Lý do tối đa 500 ký tự.")]
        public string? LyDo { get; set; }
    }

    // Một chuyến đã phân công rơi vào ngày nghỉ
    public class ChuyenTrungNgayNghi
    {
        public int LichId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public DateTime NgayVe { get; set; }
    }

    public static class XuLyDonNghi
    {
        // Chuyến (lịch chưa hủy) đã phân công cho nhân viên mà ngày đi đến ngày về giao với khoảng nghỉ.
        // Ngày về = ngày khởi hành + số ngày tour − 1.
        public static List<ChuyenTrungNgayNghi> ChuyenTrung(DuLichDbContext db, int nhanVienId, DateTime tu, DateTime den)
        {
            tu = tu.Date; den = den.Date;
            return db.PhanCongTours.AsNoTracking()
                .Where(p => (p.HuongDanVienId == nhanVienId || p.TaiXeId == nhanVienId)
                    && p.LichKhoiHanhTour!.TrangThai != TrangThaiLich.DaHuy
                    && p.LichKhoiHanhTour.NgayKhoiHanh <= den)
                .Select(p => new
                {
                    p.LichKhoiHanhTourId,
                    p.LichKhoiHanhTour!.Tour!.TenTour,
                    p.LichKhoiHanhTour.NgayKhoiHanh,
                    SoNgay = p.LichKhoiHanhTour.Tour.SoNgay ?? 1
                })
                .AsEnumerable()
                .Select(p => new ChuyenTrungNgayNghi
                {
                    LichId = p.LichKhoiHanhTourId,
                    TenTour = p.TenTour,
                    NgayDi = p.NgayKhoiHanh.Date,
                    NgayVe = p.NgayKhoiHanh.Date.AddDays(Math.Max(p.SoNgay, 1) - 1)
                })
                .Where(c => c.NgayVe >= tu)
                .OrderBy(c => c.NgayDi)
                .ToList();
        }

        // Đơn đã duyệt của nhân viên giao với khoảng ngày (dùng để chặn phân công, RB-45)
        public static DonXinNghi? NghiDaDuyetTrung(DuLichDbContext db, int nhanVienId, DateTime tu, DateTime den) =>
            db.DonXinNghis.AsNoTracking()
                .Where(d => d.NhanVienId == nhanVienId && d.TrangThai == TrangThaiDonNghi.DaDuyet
                    && d.TuNgay <= den.Date && d.DenNgay >= tu.Date)
                .OrderBy(d => d.TuNgay)
                .FirstOrDefault();
    }
}
