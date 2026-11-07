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

        // Chữ của lựa chọn "không chọn chỗ" trong ô danh sách (mặc định theo ChoTuDongXep)
        public string? NhanKhongChon { get; set; }
    }

    public class ViTriHienThi
    {
        public int Id { get; set; }
        public string MaViTri { get; set; } = string.Empty;
        public int Hang { get; set; }
        public int Cot { get; set; }
        public bool DaCoNguoi { get; set; }

        // Chuyến này không bán chỗ này (RB-37): hiện xám, không chọn được
        public bool KhongMoBan { get; set; }
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
            var khongMoBan = db.ChoKhongMoBans.Where(k => k.LichKhoiHanhTourId == lichId).Select(k => k.ViTriChoNgoiId).ToList();
            var tru = new HashSet<int>(daChon.Concat(khongMoBan).Concat(boQua));
            return db.ViTriChoNgois
                .Where(v => v.SoDoChoNgoiId == soDoId)
                .OrderBy(v => v.Hang).ThenBy(v => v.Cot)
                .Select(v => v.Id)
                .AsEnumerable()
                .Where(id => !tru.Contains(id))
                .ToList();
        }

        // Các vị trí khách được chọn trên lịch: thuộc sơ đồ của lịch (RB-18) và không nằm trong chỗ không mở bán (RB-37)
        public static HashSet<int> ViTriChonDuoc(DuLichDbContext db, int lichId, int? soDoId)
        {
            if (!soDoId.HasValue)
            {
                return new HashSet<int>();
            }
            return db.ViTriChoNgois
                .Where(v => v.SoDoChoNgoiId == soDoId.Value
                    && !db.ChoKhongMoBans.Any(k => k.LichKhoiHanhTourId == lichId && k.ViTriChoNgoiId == v.Id))
                .Select(v => v.Id)
                .ToHashSet();
        }

        // Mỗi ghế đang giữ của lịch có sơ đồ xe phải có một vị trí trên xe (RB-36).
        // Hàm này xếp chỗ trống đầu tiên cho các ghế còn thiếu: ghế đơn nhóm chưa ai chọn, ghế admin thêm vào đơn,
        // ghế đặt trước khi lịch được gán xe. Người dùng vẫn đổi chỗ được sau.
        // Chỉ thêm vào DbContext, chưa SaveChanges; gọi SAU khi các ghế và chỗ đã chọn tay đã được lưu.
        // Trả về số ghế vừa được xếp chỗ.
        public static int XepChoConThieu(DuLichDbContext db, int lichId)
        {
            var soDoId = db.LichKhoiHanhTours.Where(l => l.Id == lichId).Select(l => l.SoDoChoNgoiId).FirstOrDefault();
            if (!soDoId.HasValue)
            {
                return 0;
            }

            var dsGheThieu = db.HanhKhachDatTours
                .Where(h => h.NgayNha == null
                    && h.DatTour!.LichKhoiHanhTourId == lichId
                    && h.DatTour.TrangThai != "DaHuy"
                    && !db.ChoNgoiChuyenDis.Any(c => c.HanhKhachDatTourId == h.Id))
                .OrderBy(h => h.DatTourId).ThenBy(h => h.Id) // ghế cùng đơn nằm cạnh nhau
                .Select(h => h.Id)
                .ToList();
            if (dsGheThieu.Count == 0)
            {
                return 0;
            }

            var dsTrong = ViTriTrong(db, lichId, soDoId.Value, Enumerable.Empty<int>());
            int soXep = Math.Min(dsGheThieu.Count, dsTrong.Count);
            for (int i = 0; i < soXep; i++)
            {
                db.ChoNgoiChuyenDis.Add(new ChoNgoiChuyenDi
                {
                    LichKhoiHanhTourId = lichId,
                    ViTriChoNgoiId = dsTrong[i],
                    HanhKhachDatTourId = dsGheThieu[i]
                });
            }
            return soXep;
        }
    }
}
