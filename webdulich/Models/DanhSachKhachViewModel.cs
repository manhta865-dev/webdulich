namespace webdulich.Models
{
    // Danh sách hành khách của một chuyến cho hướng dẫn viên và tài xế được phân công (RB-41). Chỉ đọc, không lưu.
    public class DanhSachKhachViewModel
    {
        public int LichId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayKhoiHanh { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public string? TenXe { get; set; }
        public bool LaHuongDanVien { get; set; }
        public List<HanhKhachTrenChuyen> DsKhach { get; set; } = new();

        public int SoNguoiLon => DsKhach.Count(k => k.LoaiHanhKhach != "Trẻ nhỏ");
        public int SoTreNho => DsKhach.Count(k => k.LoaiHanhKhach == "Trẻ nhỏ");
        public int SoDaTra => DsKhach.Count(k => k.ThanhToan == ThanhToanKhach.DaTra);
        public int SoChuaTra => DsKhach.Count - SoDaTra;
    }

    public class HanhKhachTrenChuyen
    {
        public string? HoTen { get; set; }
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public string? MaCho { get; set; }
        public int HangCho { get; set; } = int.MaxValue;
        public int CotCho { get; set; } = int.MaxValue;

        // Chỉ hướng dẫn viên thấy, và chỉ 4 số cuối (RB-41)
        public string? GiayToRutGon { get; set; }

        public string MaChuyenDi { get; set; } = string.Empty;
        public bool LaDonNhom { get; set; }
        public string NguoiLienHe { get; set; } = string.Empty;
        public string DienThoaiLienHe { get; set; } = string.Empty;
        public string ThanhToan { get; set; } = ThanhToanKhach.ChuaTra;
        public DateTime? HanGiuCho { get; set; }
    }

    public static class ThanhToanKhach
    {
        public const string DaTra = "Đã trả";
        public const string DangKiemTra = "Đang kiểm tra";
        public const string ChuaTra = "Chưa trả";

        // "012345678901" thành "•••• 8901"; quá ngắn thì chỉ hiện dấu chấm
        public static string? RutGonGiayTo(string? so)
        {
            if (string.IsNullOrWhiteSpace(so))
            {
                return null;
            }
            var s = so.Trim();
            return s.Length > 4 ? "•••• " + s[^4..] : "••••";
        }
    }
}
