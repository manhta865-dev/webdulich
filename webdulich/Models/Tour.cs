using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace webdulich.Models
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        public string TenTour { get; set; } = string.Empty;

        public decimal Gia { get; set; }

        public string? HinhAnh { get; set; }

        public string? MoTa { get; set; }

        public int? DiaDiemId { get; set; }

        public int? DanhMucId { get; set; }

        public int? SoNgay { get; set; }

        public int? PhuongTienId { get; set; }

        // Ngừng bán thay cho xóa tour (bước mở rộng)
        public bool NgungBan { get; set; }

        [ForeignKey("DiaDiemId")]
        public DiaDiem? DiaDiemInfo { get; set; }

        [ForeignKey("DanhMucId")]
        public DanhMuc? DanhMuc { get; set; }

        [ForeignKey("PhuongTienId")]
        public PhuongTien? PhuongTienInfo { get; set; }

        public ICollection<TourLoaiDiaHinh> TourLoaiDiaHinhs { get; set; } = new List<TourLoaiDiaHinh>();

        public ICollection<TourPhongCach> TourPhongCachs { get; set; } = new List<TourPhongCach>();

        public ICollection<LichKhoiHanhTour> LichKhoiHanhTours { get; set; } = new List<LichKhoiHanhTour>();

        public ICollection<LichTrinhTour> LichTrinhTours { get; set; } = new List<LichTrinhTour>();

        public ICollection<TourHinhAnh> TourHinhAnhs { get; set; } = new List<TourHinhAnh>();

        [NotMapped]
        public string DiaDiem { get; set; } = string.Empty;

        [NotMapped]
        public string? LoaiTour { get; set; }

        [NotMapped]
        public string? PhuongTien { get; set; }

        [NotMapped]
        public string? LoaiDiaHinh { get; set; }

        [NotMapped]
        public string? PhongCach { get; set; }

        [NotMapped]
        public List<string> LoaiDiaHinhDaChon { get; set; } = new List<string>();

        [NotMapped]
        public List<string> PhongCachDaChon { get; set; } = new List<string>();

        [NotMapped]
        public IFormFile? HinhFile { get; set; }

        [NotMapped]
        public List<IFormFile> HinhPhuFiles { get; set; } = new List<IFormFile>();

        [NotMapped]
        public string TenDiaDiemHienThi =>
            DiaDiemInfo != null && !string.IsNullOrWhiteSpace(DiaDiemInfo.TenDiaDiem)
                ? DiaDiemInfo.TenDiaDiem
                : "Chưa cập nhật";

        [NotMapped]
        public string PhuongTienHienThi =>
            PhuongTienInfo != null && !string.IsNullOrWhiteSpace(PhuongTienInfo.TenPhuongTien)
                ? PhuongTienInfo.TenPhuongTien
                : "Chưa cập nhật";

        [NotMapped]
        public string LoaiDiaHinhHienThi
        {
            get
            {
                var ds = TourLoaiDiaHinhs?
                    .Where(x => x.LoaiDiaHinh != null)
                    .Select(x => x.LoaiDiaHinh!.TenLoaiDiaHinh)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList() ?? new List<string>();

                return ds.Any() ? string.Join(", ", ds) : "Chưa cập nhật";
            }
        }

        [NotMapped]
        public string PhongCachHienThi
        {
            get
            {
                var ds = TourPhongCachs?
                    .Where(x => x.PhongCach != null)
                    .Select(x => x.PhongCach!.TenPhongCach)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList() ?? new List<string>();

                return ds.Any() ? string.Join(", ", ds) : "Chưa cập nhật";
            }
        }
    }
}