namespace webdulich.Models
{
    // Trang chọn chỗ ngồi: sơ đồ của lịch, chỗ đã có người, và chỗ đang chọn cho từng hành khách
    public class ChonChoNgoiViewModel
    {
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public string TenSoDo { get; set; } = string.Empty;
        public bool LaMayBay { get; set; }
        public int SoHang { get; set; }
        public int SoCot { get; set; }
        public List<ViTriHienThi> DsViTri { get; set; } = new List<ViTriHienThi>();
        public List<HanhKhachChonCho> DsHanhKhach { get; set; } = new List<HanhKhachChonCho>();

        // Lưới đổi chỗ sau khi đặt (Lịch sử) dùng chung partial; khi đó gửi về action khác
        public string ActionLuu { get; set; } = "ChonChoNgoi";
        public int? DatTourId { get; set; }
        public bool ChoTuDongXep { get; set; } = true;
    }

    public class ViTriHienThi
    {
        public int Id { get; set; }
        public string MaViTri { get; set; } = string.Empty;
        public int Hang { get; set; }
        public int Cot { get; set; }
        public bool DaCoNguoi { get; set; }
    }

    public class HanhKhachChonCho
    {
        public int ThuTu { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public int? ViTriId { get; set; }
    }

    // Quy tắc chung khi gán chỗ ngồi cho ghế (RB-08, RB-18)
    public static class XepChoNgoi
    {
        // Các vị trí của sơ đồ còn trống trên chuyến, theo thứ tự hàng rồi cột (dùng khi để hệ thống xếp)
        public static List<int> ViTriTrong(DuLichDbContext db, int lichId, int soDoId, IEnumerable<int> boQua)
        {
            var daChon = db.ChoNgoiChuyenDis.Where(c => c.LichKhoiHanhTourId == lichId).Select(c => c.ViTriChoNgoiId).ToList();
            var tru = new HashSet<int>(daChon.Concat(boQua));
            return db.ViTriChoNgois
                .Where(v => v.SoDoChoNgoiId == soDoId)
                .OrderBy(v => v.Hang).ThenBy(v => v.Cot)
                .Select(v => v.Id)
                .AsEnumerable()
                .Where(id => !tru.Contains(id))
                .ToList();
        }
    }
}
