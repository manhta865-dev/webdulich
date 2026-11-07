using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace webdulich.Models
{
    // Vị trí trên xe mà chuyến này không bán: ghế hướng dẫn viên, ghế hỏng, hoặc công ty muốn đi đoàn nhỏ (RB-37).
    // Khóa chính ghép (LichKhoiHanhTourId, ViTriChoNgoiId), bảng chỉ có khóa nên đạt BCNF.
    // Số chỗ mở bán của lịch không lưu: bằng số vị trí của xe trừ số dòng của lịch trong bảng này (SucChuaLich).
    public class ChoKhongMoBan
    {
        public int LichKhoiHanhTourId { get; set; }

        public int ViTriChoNgoiId { get; set; }

        [ForeignKey("LichKhoiHanhTourId")]
        public LichKhoiHanhTour? LichKhoiHanhTour { get; set; }

        [ForeignKey("ViTriChoNgoiId")]
        public ViTriChoNgoi? ViTriChoNgoi { get; set; }
    }

    // Tính sức chứa của lịch từ xe và các chỗ không mở bán (RB-14, RB-37)
    public static class SucChuaLich
    {
        // Số chỗ mở bán của một lịch = số vị trí của xe − số chỗ không mở bán
        public static int SoChoMoBan(DuLichDbContext db, int lichId)
        {
            var soDoId = db.LichKhoiHanhTours.Where(l => l.Id == lichId).Select(l => l.SoDoChoNgoiId).FirstOrDefault();
            if (!soDoId.HasValue)
            {
                return 0;
            }
            int soViTri = db.ViTriChoNgois.Count(v => v.SoDoChoNgoiId == soDoId.Value);
            int soKhoa = db.ChoKhongMoBans.Count(k => k.LichKhoiHanhTourId == lichId);
            return soViTri - soKhoa;
        }

        // Số chỗ mở bán của nhiều lịch một lần (trang danh sách lịch)
        public static Dictionary<int, int> SoChoMoBan(DuLichDbContext db, IEnumerable<int> dsLichId)
        {
            var ds = dsLichId.ToList();
            return db.LichKhoiHanhTours
                .Where(l => ds.Contains(l.Id))
                .Select(l => new
                {
                    l.Id,
                    SoViTri = db.ViTriChoNgois.Count(v => v.SoDoChoNgoiId == l.SoDoChoNgoiId),
                    SoKhoa = db.ChoKhongMoBans.Count(k => k.LichKhoiHanhTourId == l.Id)
                })
                .ToDictionary(x => x.Id, x => x.SoViTri - x.SoKhoa);
        }

        // Các vị trí không mở bán của lịch
        public static HashSet<int> ViTriKhoa(DuLichDbContext db, int lichId) =>
            db.ChoKhongMoBans.Where(k => k.LichKhoiHanhTourId == lichId).Select(k => k.ViTriChoNgoiId).ToHashSet();

        // Số ghế đang giữ trên lịch: ghế chưa nhả của đơn chưa hủy (RB-12)
        public static int SoGheDangGiu(DuLichDbContext db, int lichId) =>
            db.HanhKhachDatTours.Count(h => h.NgayNha == null
                && h.DatTour!.LichKhoiHanhTourId == lichId
                && h.DatTour.TrangThai != "DaHuy");

        // Tính lại số chỗ còn của lịch trong MỘT câu UPDATE: (số vị trí xe − chỗ không mở bán) − ghế đang giữ (RB-12).
        // Gọi sau khi đã lưu thay đổi về xe hoặc chỗ khóa, trong cùng giao dịch. Kết quả âm (khóa quá nhiều chỗ so với
        // số khách đã giữ) bị CHECK CK_Lich_SoCho chặn nên giao dịch lỗi, không lưu được dữ liệu sai.
        public static void TinhLaiSoChoCon(DuLichDbContext db, int lichId)
        {
            db.Database.ExecuteSqlInterpolated($@"
UPDATE l SET
    SoChoCon = x.MoBan - x.DangGiu
FROM LichKhoiHanhTours l
CROSS APPLY (SELECT
    (SELECT COUNT(*) FROM ViTriChoNgois v WHERE v.SoDoChoNgoiId = l.SoDoChoNgoiId)
      - (SELECT COUNT(*) FROM ChoKhongMoBans k WHERE k.LichKhoiHanhTourId = l.Id) AS MoBan,
    (SELECT COUNT(*) FROM HanhKhachDatTours h JOIN DatTours d ON d.Id = h.DatTourId
      WHERE d.LichKhoiHanhTourId = l.Id AND d.TrangThai <> 'DaHuy' AND h.NgayNha IS NULL) AS DangGiu) x
WHERE l.Id = {lichId};");
        }

        // Chọn các vị trí cần khóa để chỉ mở bán soMoBan chỗ: lấy các chỗ trống ở cuối xe (hàng sau trước),
        // không bao giờ khóa chỗ đã có khách ngồi. Trả về null nếu không đủ chỗ trống để khóa.
        public static List<int>? ChonChoKhoaCuoiXe(DuLichDbContext db, int lichId, int soDoId, int soMoBan)
        {
            var dsViTri = db.ViTriChoNgois
                .Where(v => v.SoDoChoNgoiId == soDoId)
                .OrderByDescending(v => v.Hang).ThenByDescending(v => v.Cot)
                .Select(v => v.Id)
                .ToList();
            int canKhoa = dsViTri.Count - soMoBan;
            if (canKhoa <= 0)
            {
                return new List<int>();
            }
            var coNguoi = db.ChoNgoiChuyenDis.Where(c => c.LichKhoiHanhTourId == lichId).Select(c => c.ViTriChoNgoiId).ToHashSet();
            var dsKhoa = dsViTri.Where(id => !coNguoi.Contains(id)).Take(canKhoa).ToList();
            return dsKhoa.Count == canKhoa ? dsKhoa : null;
        }
    }
}
