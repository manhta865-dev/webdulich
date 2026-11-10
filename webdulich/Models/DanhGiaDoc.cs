using Microsoft.EntityFrameworkCore;

namespace webdulich.Models
{
    // Truy vấn dùng chung cho đánh giá tour (RB-43).
    public static class DanhGiaDoc
    {
        // Các lịch tài khoản được đánh giá: có ghế chưa nhả, đã trả tiền, đơn không hủy, lịch không hủy,
        // và chuyến đã về (ngày khởi hành + số ngày của tour đã qua).
        // Chủ ghế: đơn nhóm là người nhận ghế (TaiKhoanId của ghế); đơn cá nhân và đặt tại quầy là người đặt đơn.
        // Đã trả: giao dịch thành công của ghế (đơn nhóm) hoặc của cả đơn (đơn cá nhân, tại quầy).
        public static IQueryable<int> LichDuocDanhGia(DuLichDbContext db, int taiKhoanId)
        {
            var homNay = GioVN.HomNay;
            return db.HanhKhachDatTours
                .Where(h => h.NgayNha == null
                    && h.DatTour!.TrangThai != "DaHuy"
                    && h.DatTour.LichKhoiHanhTour!.TrangThai != TrangThaiLich.DaHuy
                    && EF.Functions.DateDiffDay(h.DatTour.LichKhoiHanhTour.NgayKhoiHanh, homNay) >= (h.DatTour.LichKhoiHanhTour.Tour!.SoNgay ?? 1)
                    && (h.TaiKhoanId == taiKhoanId || (h.DatTour.LoaiDon == "CaNhan" && h.TaiKhoanId == null && h.DatTour.TaiKhoanId == taiKhoanId))
                    && (db.GiaoDichThanhToans.Any(g => g.HanhKhachDatTourId == h.Id && g.TrangThai == "ThanhCong")
                        || db.GiaoDichThanhToans.Any(g => g.DatTourId == h.DatTourId && g.TrangThai == "ThanhCong")))
                .Select(h => h.DatTour!.LichKhoiHanhTourId)
                .Distinct();
        }

        // Điểm trung bình và số lượt của các đánh giá đang hiện, theo tour (dữ liệu dẫn xuất, không lưu)
        public static (double TrungBinh, int SoLuot) TongHop(DuLichDbContext db, int tourId)
        {
            var ds = db.DanhGiaTours.Where(d => d.DangHien && d.LichKhoiHanhTour!.TourId == tourId).Select(d => d.SoSao);
            int soLuot = ds.Count();
            return (soLuot == 0 ? 0 : ds.Average(s => (double)s), soLuot);
        }

        // Tên hiện công khai: họ tên rút gọn để không lộ đầy đủ ("Nguyễn Văn An" thành "Nguyễn V. An")
        public static string TenRutGon(string? hoTen)
        {
            var phan = (hoTen ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (phan.Length == 0) return "Khách TravelVN";
            if (phan.Length <= 2) return string.Join(' ', phan);
            return phan[0] + " " + string.Join(' ', phan.Skip(1).Take(phan.Length - 2).Select(p => p[0] + ".")) + " " + phan[^1];
        }
    }

    // Một chuyến đã đi xong ở trang Lịch sử, kèm đánh giá (nếu đã viết)
    public class ChuyenDaDi
    {
        public int LichId { get; set; }
        public int TourId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public string? Anh { get; set; }
        public DateTime NgayDi { get; set; }
        public int? SoSao { get; set; }
        public bool DangHien { get; set; } = true;
    }
}
