using Microsoft.EntityFrameworkCore;
using webdulich.Services;

namespace webdulich.Models
{
    // Danh sách tour đang bán gửi cho Gemini (trang Gợi ý tour, khung Trợ lý). Chỉ thông tin công khai trên trang tour;
    // giá từ lấy giá người lớn thấp nhất của các lịch sắp tới còn chỗ, không có lịch thì giá tour. Chỉ đọc.
    public static class TourChoAiDoc
    {
        public static List<TourChoAi> LayDanhSach(DuLichDbContext db)
        {
            var homNay = GioVN.HomNay;
            var dsTour = db.Tours.AsNoTracking()
                .Where(x => !x.NgungBan)
                .Include(x => x.DiaDiemInfo)
                .Include(x => x.PhuongTienInfo)
                .Include(x => x.TourLoaiDiaHinhs).ThenInclude(x => x.LoaiDiaHinh)
                .Include(x => x.TourPhongCachs).ThenInclude(x => x.PhongCach)
                .AsSplitQuery()
                .ToList();
            var dsLich = db.LichKhoiHanhTours.AsNoTracking()
                .Where(l => l.TrangThai == TrangThaiLich.MoBan && l.NgayKhoiHanh > homNay && l.SoChoCon > 0)
                .Select(l => new { l.TourId, l.GiaNguoiLon })
                .ToList()
                .GroupBy(l => l.TourId)
                .ToDictionary(g => g.Key, g => (SoLich: g.Count(), GiaThapNhat: g.Min(x => x.GiaNguoiLon)));

            return dsTour.Select(t =>
            {
                var coLich = dsLich.TryGetValue(t.Id, out var lich);
                var moTa = t.MoTa ?? string.Empty;
                return new TourChoAi
                {
                    Id = t.Id,
                    Ten = t.TenTour.Trim(),
                    DiemDen = t.TenDiaDiemHienThi,
                    SoNgay = t.SoNgay,
                    GiaTu = coLich && lich.GiaThapNhat.HasValue ? lich.GiaThapNhat.Value : t.Gia,
                    DiaHinh = t.LoaiDiaHinhHienThi,
                    PhongCach = t.PhongCachHienThi,
                    PhuongTien = t.PhuongTienHienThi,
                    MoTa = moTa.Length > 200 ? moTa[..200] : moTa,
                    SoLichSapToi = coLich ? lich.SoLich : 0
                };
            }).ToList();
        }
    }
}
