using Microsoft.EntityFrameworkCore;

namespace webdulich.Models
{
    // Một chuyến được tính công trong tháng
    public class ChuyenTinhCong
    {
        public int LichId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public DateTime NgayVe { get; set; }
        public string VaiTro { get; set; } = string.Empty;
        // Số ngày của chuyến rơi vào tháng đang tính (chuyến vắt qua hai tháng thì chia theo ngày)
        public int SoNgayTrongThang { get; set; }
    }

    // Phiếu lương một tháng của một nhân viên: tạm tính từ dữ liệu thật, hoặc lấy từ bản đã chốt
    public class PhieuLuong
    {
        public NhanVien NhanVien { get; set; } = new();
        public int Nam { get; set; }
        public int Thang { get; set; }
        public bool CoMucLuong { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal TienCongNgay { get; set; }
        public int SoNgayDiTour { get; set; }
        public int SoNgayNghi { get; set; }
        public int SoNgayNghiTruLuong { get; set; }
        public int SoNgayCongChuan { get; set; }
        public int SoNgayPhepNam { get; set; }
        // Số ngày phép năm đã dùng tính đến hết tháng này
        public int SoNgayPhepDaDung { get; set; }
        public List<ChuyenTinhCong> DsChuyen { get; set; } = new();
        public List<DonXinNghi> DsDonNghi { get; set; } = new();
        public List<KhoanLuong> DsKhoan { get; set; } = new();
        public BangLuong? BangLuong { get; set; }

        public bool DaChot => BangLuong != null;
        public bool DaTra => BangLuong?.NgayTra != null;
        public decimal TienCong => SoNgayDiTour * TienCongNgay;
        public decimal TienTruNghi => SoNgayCongChuan > 0 ? Math.Round(LuongCoBan * SoNgayNghiTruLuong / SoNgayCongChuan, 0) : 0;
        public decimal TongKhoan => DsKhoan.Sum(k => k.SoTien);
        public decimal Tong => LuongCoBan + TienCong - TienTruNghi + TongKhoan;
        public string TrangThai => DaTra ? "Đã trả" : DaChot ? "Đã chốt" : "Tạm tính";
    }

    // Tính lương tháng (RB-46). Lương = lương cơ bản + ngày đi tour × tiền công ngày − tiền trừ nghỉ + thưởng/phạt.
    public static class TinhLuong
    {
        public static int SoNgayPhepNam(IConfiguration c) => Math.Max(c.GetValue("Luong:SoNgayPhepNam", 12), 0);
        public static int SoNgayCongChuan(IConfiguration c) => Math.Max(c.GetValue("Luong:SoNgayCongChuan", 26), 1);

        public static PhieuLuong Tinh(DuLichDbContext db, NhanVien nv, int nam, int thang, int soNgayPhepNam, int soNgayCongChuan)
        {
            var dauThang = new DateTime(nam, thang, 1);
            var cuoiThang = dauThang.AddMonths(1).AddDays(-1);
            var p = new PhieuLuong { NhanVien = nv, Nam = nam, Thang = thang };

            p.DsChuyen = ChuyenTrongThang(db, nv.Id, dauThang, cuoiThang);
            p.DsKhoan = db.KhoanLuongs.AsNoTracking().Include(k => k.NguoiTao)
                .Where(k => k.NhanVienId == nv.Id && k.Nam == nam && k.Thang == thang)
                .OrderBy(k => k.NgayTao).ToList();

            // Ngày nghỉ đã duyệt trong năm, xếp theo thời gian: soNgayPhepNam ngày đầu không trừ, từ ngày sau đó trừ lương
            var dauNam = new DateTime(nam, 1, 1);
            var cuoiNam = new DateTime(nam, 12, 31);
            var dsDonNam = db.DonXinNghis.AsNoTracking()
                .Where(d => d.NhanVienId == nv.Id && d.TrangThai == TrangThaiDonNghi.DaDuyet && d.TuNgay <= cuoiNam && d.DenNgay >= dauNam)
                .OrderBy(d => d.TuNgay).ToList();
            var ngayNghi = dsDonNam
                .SelectMany(d => Enumerable.Range(0, (d.DenNgay.Date - d.TuNgay.Date).Days + 1).Select(i => d.TuNgay.Date.AddDays(i)))
                .Where(n => n >= dauNam && n <= cuoiNam)
                .Distinct().OrderBy(n => n).ToList();
            p.SoNgayNghi = ngayNghi.Count(n => n >= dauThang && n <= cuoiThang);
            p.SoNgayNghiTruLuong = ngayNghi.Select((n, i) => (n, i)).Count(x => x.i >= soNgayPhepNam && x.n >= dauThang && x.n <= cuoiThang);
            p.SoNgayPhepDaDung = Math.Min(ngayNghi.Count(n => n <= cuoiThang), soNgayPhepNam);
            p.DsDonNghi = dsDonNam.Where(d => d.TuNgay <= cuoiThang && d.DenNgay >= dauThang).ToList();
            p.SoNgayPhepNam = soNgayPhepNam;

            p.BangLuong = db.BangLuongs.AsNoTracking().Include(b => b.NguoiChot).Include(b => b.NguoiTra)
                .FirstOrDefault(b => b.NhanVienId == nv.Id && b.Nam == nam && b.Thang == thang);
            if (p.BangLuong != null)
            {
                // Đã chốt: số liệu lấy từ bản chụp lúc chốt, không theo dữ liệu hiện tại
                var b = p.BangLuong;
                p.CoMucLuong = true;
                p.LuongCoBan = b.LuongCoBan;
                p.TienCongNgay = b.TienCongNgay;
                p.SoNgayDiTour = b.SoNgayDiTour;
                p.SoNgayNghiTruLuong = b.SoNgayNghiTruLuong;
                p.SoNgayCongChuan = b.SoNgayCongChuan;
                return p;
            }

            // Mức lương áp dụng: dòng mới nhất có ngày áp dụng không sau cuối tháng
            var muc = db.MucLuongNhanViens.AsNoTracking()
                .Where(m => m.NhanVienId == nv.Id && m.ApDungTu <= cuoiThang)
                .OrderByDescending(m => m.ApDungTu).FirstOrDefault();
            p.CoMucLuong = muc != null;
            p.LuongCoBan = muc?.LuongCoBan ?? 0;
            p.TienCongNgay = muc?.TienCongNgay ?? 0;
            p.SoNgayDiTour = p.DsChuyen.Sum(c => c.SoNgayTrongThang);
            p.SoNgayCongChuan = soNgayCongChuan;
            return p;
        }

        // Chuyến được phân công (lịch chưa hủy) có ngày rơi vào khoảng [tu, den]; mỗi ngày tính một công
        public static List<ChuyenTinhCong> ChuyenTrongThang(DuLichDbContext db, int nhanVienId, DateTime tu, DateTime den)
        {
            return db.PhanCongTours.AsNoTracking()
                .Where(x => (x.HuongDanVienId == nhanVienId || x.TaiXeId == nhanVienId)
                    && x.LichKhoiHanhTour!.TrangThai != TrangThaiLich.DaHuy
                    && x.LichKhoiHanhTour.NgayKhoiHanh <= den)
                .Select(x => new
                {
                    x.LichKhoiHanhTourId,
                    x.HuongDanVienId,
                    x.LichKhoiHanhTour!.Tour!.TenTour,
                    x.LichKhoiHanhTour.NgayKhoiHanh,
                    SoNgay = x.LichKhoiHanhTour.Tour.SoNgay ?? 1
                })
                .AsEnumerable()
                .Select(x =>
                {
                    var di = x.NgayKhoiHanh.Date;
                    var ve = di.AddDays(Math.Max(x.SoNgay, 1) - 1);
                    var batDau = di > tu ? di : tu;
                    var ketThuc = ve < den ? ve : den;
                    return new ChuyenTinhCong
                    {
                        LichId = x.LichKhoiHanhTourId,
                        TenTour = x.TenTour,
                        NgayDi = di,
                        NgayVe = ve,
                        VaiTro = x.HuongDanVienId == nhanVienId ? "Hướng dẫn viên" : "Tài xế",
                        SoNgayTrongThang = Math.Max((ketThuc - batDau).Days + 1, 0)
                    };
                })
                .Where(c => c.SoNgayTrongThang > 0)
                .OrderBy(c => c.NgayDi)
                .ToList();
        }
    }
}
