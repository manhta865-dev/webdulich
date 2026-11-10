using Microsoft.EntityFrameworkCore;

namespace webdulich.Models
{
    // Một tour trên Bản đồ tour (DiaDiem/BanDo) và quả địa cầu trang chủ. Chỉ đọc, tính từ tour và lịch khởi hành sắp tới.
    public class TourTrenBanDo
    {
        public int Id { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public string DiemDen { get; set; } = string.Empty;
        public string Vung { get; set; } = string.Empty;
        public string? Anh { get; set; }
        public double Vi { get; set; }
        public double Kinh { get; set; }
        public decimal? GiaTu { get; set; }
        public DateTime? NgayGanNhat { get; set; }
        public int SoLich { get; set; }

        public const string VungNuocNgoai = "Tour nước ngoài";

        // Mọi tour đang bán, chia hai nhóm (RB-44):
        //  CoViTri: có vị trí, đặt chấm trên bản đồ và quả địa cầu. Vùng: Miền Bắc, Miền Trung và Tây Nguyên, Miền Nam
        //   theo vĩ độ; tour danh mục "Ngoài nước" vào vùng "Tour nước ngoài";
        //  ChuaCoViTri: địa điểm chưa được chọn vị trí (admin cần bấm chọn ở form tour).
        // Vị trí: điểm cuối lịch trình có tọa độ (Mo10, chỉ trong nước), không có thì vị trí của địa điểm (DiaDiems.ViDo, KinhDo, Mo18).
        // Admin thêm tour và chọn vị trí là tự có ở đây, không phải sửa code.
        public static (List<TourTrenBanDo> CoViTri, List<TourTrenBanDo> ChuaCoViTri) LayTatCa(DuLichDbContext db)
        {
            var homNay = GioVN.HomNay;
            var dsTour = db.Tours.AsNoTracking()
                .Where(t => !t.NgungBan)
                .Select(t => new
                {
                    t.Id, t.TenTour, t.HinhAnh, DiaDiem = t.DiaDiemInfo != null ? t.DiaDiemInfo.TenDiaDiem : null,
                    ViDoDiaDiem = t.DiaDiemInfo != null ? t.DiaDiemInfo.ViDo : null,
                    KinhDoDiaDiem = t.DiaDiemInfo != null ? t.DiaDiemInfo.KinhDo : null,
                    NuocNgoai = t.DanhMuc != null && t.DanhMuc.TenDanhMuc == "Ngoài nước",
                    DiemCuoi = db.LichTrinhTours.Where(l => l.TourId == t.Id && l.ViDo != null && l.KinhDo != null)
                        .OrderByDescending(l => l.NgayThu).Select(l => new { l.ViDo, l.KinhDo }).FirstOrDefault(),
                    DsLich = db.LichKhoiHanhTours
                        .Where(l => l.TourId == t.Id && l.TrangThai == TrangThaiLich.MoBan && l.NgayKhoiHanh > homNay && l.SoChoCon > 0)
                        .Select(l => new { l.NgayKhoiHanh, Gia = l.GiaNguoiLon ?? t.Gia }).ToList()
                })
                .ToList();

            var coViTri = new List<TourTrenBanDo>();
            var chuaCo = new List<TourTrenBanDo>();
            foreach (var t in dsTour)
            {
                (double, double)? td = t.DiemCuoi != null ? ((double)t.DiemCuoi.ViDo!.Value, (double)t.DiemCuoi.KinhDo!.Value)
                    : t.ViDoDiaDiem.HasValue && t.KinhDoDiaDiem.HasValue ? ((double)t.ViDoDiaDiem.Value, (double)t.KinhDoDiaDiem.Value)
                    : null;
                var tour = new TourTrenBanDo
                {
                    Id = t.Id,
                    TenTour = t.TenTour,
                    DiemDen = ToaDoDiaDanh.TenHienThi(t.DiaDiem ?? t.TenTour),
                    Anh = t.HinhAnh,
                    GiaTu = t.DsLich.Any() ? t.DsLich.Min(l => l.Gia) : null,
                    NgayGanNhat = t.DsLich.Any() ? t.DsLich.Min(l => l.NgayKhoiHanh) : null,
                    SoLich = t.DsLich.Count
                };
                if (td == null)
                {
                    chuaCo.Add(tour);
                    continue;
                }
                tour.Vi = td.Value.Item1;
                tour.Kinh = td.Value.Item2;
                tour.Vung = t.NuocNgoai ? VungNuocNgoai : ToaDoDiaDanh.Vung(tour.Vi);
                coViTri.Add(tour);
            }
            coViTri = coViTri
                .OrderBy(x => x.Vung == "Miền Bắc" ? 0 : x.Vung == "Miền Nam" ? 2 : x.Vung == VungNuocNgoai ? 3 : 1)
                .ThenByDescending(x => x.Vi).ToList();
            return (coViTri, chuaCo);
        }
    }

    // Lịch khởi hành theo tháng (DiaDiem/LichKhoiHanh)
    public class LichThangViewModel
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int? DiaDiemId { get; set; }
        public List<(int Id, string Ten)> DsDiaDiem { get; set; } = new();
        public List<LichTrongNgay> DsLich { get; set; } = new();
        public DateTime DauThang => new(Nam, Thang, 1);
    }

    public class LichTrongNgay
    {
        public int LichId { get; set; }
        public int TourId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public DateTime Ngay { get; set; }
        public TimeSpan? Gio { get; set; }
        public int SoChoCon { get; set; }
        public decimal Gia { get; set; }
        public bool DatDuoc { get; set; }

        // Mức còn chỗ để tô màu: con-nhieu, sap-het, het
        public string MucCho => SoChoCon <= 0 ? "het" : SoChoCon <= 5 ? "sap-het" : "con-nhieu";
    }
}
