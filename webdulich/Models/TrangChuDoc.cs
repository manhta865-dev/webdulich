using Microsoft.EntityFrameworkCore;

namespace webdulich.Models
{
    // Dữ liệu thật cho hai khối trang chủ: bảng giờ khởi hành và bưu thiếp đánh giá. Chỉ đọc.
    public static class TrangChuDoc
    {
        // Các lịch đang mở bán, đi từ ngày mai trở đi, của tour còn bán; xếp theo ngày và giờ đi.
        // Số chỗ mở bán tính theo RB-37 để biết "sắp hết".
        public static List<ChuyenSapDi> LichSapDi(DuLichDbContext db, int soLuong)
        {
            var homNay = GioVN.HomNay;
            var ds = db.LichKhoiHanhTours.AsNoTracking()
                .Where(l => l.TrangThai == TrangThaiLich.MoBan && l.NgayKhoiHanh > homNay && l.Tour != null && !l.Tour.NgungBan)
                .OrderBy(l => l.NgayKhoiHanh).ThenBy(l => l.GioKhoiHanh).ThenBy(l => l.Id)
                .Take(soLuong)
                .Select(l => new ChuyenSapDi
                {
                    LichId = l.Id,
                    TourId = l.TourId,
                    TenTour = l.Tour!.TenTour,
                    NgayKhoiHanh = l.NgayKhoiHanh,
                    GioKhoiHanh = l.GioKhoiHanh,
                    SoChoCon = l.SoChoCon
                })
                .ToList();

            var moBan = SucChuaLich.SoChoMoBan(db, ds.Select(x => x.LichId));
            foreach (var c in ds)
            {
                c.SoChoMoBan = moBan.TryGetValue(c.LichId, out var mb) ? mb : 0;
            }
            return ds;
        }

        // Đánh giá đang hiện, có nội dung, mới nhất; tên rút gọn như trang tour
        public static List<BuuThiepDanhGia> DanhGiaMoi(DuLichDbContext db, int soLuong)
        {
            return db.DanhGiaTours.AsNoTracking()
                .Where(d => d.DangHien && d.NoiDung != null && d.NoiDung != "")
                .OrderByDescending(d => d.NgayTao)
                .Take(soLuong)
                .Select(d => new
                {
                    d.SoSao,
                    d.NoiDung,
                    HoTen = d.TaiKhoan != null ? d.TaiKhoan.HoTen : null,
                    TenTour = d.LichKhoiHanhTour != null && d.LichKhoiHanhTour.Tour != null ? d.LichKhoiHanhTour.Tour.TenTour : "",
                    TourId = d.LichKhoiHanhTour != null ? d.LichKhoiHanhTour.TourId : 0,
                    NgayDi = d.LichKhoiHanhTour != null ? d.LichKhoiHanhTour.NgayKhoiHanh : d.NgayTao
                })
                .ToList()
                .Select(d => new BuuThiepDanhGia
                {
                    TenNguoi = DanhGiaDoc.TenRutGon(d.HoTen),
                    SoSao = d.SoSao,
                    NoiDung = d.NoiDung!.Length > 220 ? d.NoiDung[..220].TrimEnd() + "…" : d.NoiDung,
                    TenTour = d.TenTour,
                    TourId = d.TourId,
                    NgayDi = d.NgayDi
                })
                .ToList();
        }
    }

    // Một dòng trên bảng giờ khởi hành
    public class ChuyenSapDi
    {
        public int LichId { get; set; }
        public int TourId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayKhoiHanh { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public int SoChoCon { get; set; }
        public int SoChoMoBan { get; set; }

        // Nhãn trạng thái tính từ số chỗ (không lưu)
        public string TrangThai => SoChoCon <= 0 ? "Hết chỗ" : SoChoCon <= 5 ? "Sắp hết" : "Còn chỗ";
        public string LopTrangThai => SoChoCon <= 0 ? "het" : SoChoCon <= 5 ? "sap-het" : "con";
    }

    // Một tấm bưu thiếp đánh giá
    public class BuuThiepDanhGia
    {
        public string TenNguoi { get; set; } = string.Empty;
        public int SoSao { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public string TenTour { get; set; } = string.Empty;
        public int TourId { get; set; }
        public DateTime NgayDi { get; set; }
    }
}
