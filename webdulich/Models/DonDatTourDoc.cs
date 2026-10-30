using Microsoft.EntityFrameworkCore;

namespace webdulich.Models
{
    // Một đơn đã được tính sẵn để hiển thị. Mọi giá trị dẫn xuất lấy từ lịch, ghế và giao dịch
    // (docs/MO_HINH_DU_LIEU.md, mục 5), không đọc các cột cũ TourId, NgayDi, SoNguoi, TongTien,
    // TrangThaiThanhToan, PhuongThucThanhToan, MaKhuyenMai của DatTours.
    public class DonDatTourDoc
    {
        public int Id { get; set; }
        public string MaChuyenDi { get; set; } = string.Empty;
        public string TenKhach { get; set; } = string.Empty;
        public string DienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DiaChi { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public DateTime NgayDat { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string LoaiDon { get; set; } = string.Empty;
        public int? TaiKhoanId { get; set; }

        public int? LichKhoiHanhTourId { get; set; }
        public int TourId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public string PhuongTien { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public bool CoSoDoChoNgoi { get; set; }

        // Chủ đơn còn chọn hoặc đổi chỗ được: đơn chưa hủy, lịch có sơ đồ, chưa tới ngày đi
        public bool DuocDoiCho => !DaHuy && CoSoDoChoNgoi && NgayDi.Date > DateTime.Today;

        public int SoNguoi { get; set; }
        public int SoNguoiLon { get; set; }
        public int SoTreNho { get; set; }
        public decimal TongTienTruocGiam { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal TongTien { get; set; }
        public string? MaKhuyenMai { get; set; }

        public string PhuongThucThanhToan { get; set; } = string.Empty;
        public string TrangThaiThanhToan { get; set; } = string.Empty;

        public bool DaHuy => TrangThai == "DaHuy";
        public bool DaThanhToan => TrangThaiThanhToan == TrangThaiThanhToanDon.DaThanhToan;
    }

    // Nhãn trạng thái thanh toán hiển thị cho người dùng (tính, không lưu)
    public static class TrangThaiThanhToanDon
    {
        public const string DaThanhToan = "Đã thanh toán";
        public const string DangKiemTra = "Đang kiểm tra";
        public const string ChuaThanhToan = "Chưa thanh toán";
        public const string DaHuy = "Đã hủy";
    }

    public static class TruyVanDon
    {
        // Chuyển truy vấn DatTours thành DonDatTourDoc; EF dịch toàn bộ sang một câu SQL.
        // Đơn đã hủy thì ghế đều đã nhả, nên số người và tiền tính trên mọi ghế để còn xem lại được.
        public static IQueryable<DonDatTourDoc> ChonDon(this IQueryable<DatTours> ds, DuLichDbContext db)
        {
            return ds.Select(x => new DonDatTourDoc
            {
                Id = x.Id,
                MaChuyenDi = x.MaChuyenDi,
                TenKhach = x.TenKhach,
                DienThoai = x.DienThoai,
                Email = x.Email,
                DiaChi = x.DiaChi,
                GhiChu = x.GhiChu,
                NgayDat = x.NgayDat,
                TrangThai = x.TrangThai,
                LoaiDon = x.LoaiDon,
                TaiKhoanId = x.TaiKhoanId,

                LichKhoiHanhTourId = x.LichKhoiHanhTourId,
                TourId = x.LichKhoiHanhTour != null ? x.LichKhoiHanhTour.TourId : 0,
                TenTour = x.LichKhoiHanhTour != null && x.LichKhoiHanhTour.Tour != null ? x.LichKhoiHanhTour.Tour.TenTour : "Chưa cập nhật",
                PhuongTien = x.LichKhoiHanhTour != null && x.LichKhoiHanhTour.Tour != null && x.LichKhoiHanhTour.Tour.PhuongTienInfo != null
                    ? x.LichKhoiHanhTour.Tour.PhuongTienInfo.TenPhuongTien : "Chưa cập nhật",
                NgayDi = x.LichKhoiHanhTour != null ? x.LichKhoiHanhTour.NgayKhoiHanh : x.NgayDat,
                GioKhoiHanh = x.LichKhoiHanhTour != null ? x.LichKhoiHanhTour.GioKhoiHanh : null,
                CoSoDoChoNgoi = x.LichKhoiHanhTour != null && x.LichKhoiHanhTour.SoDoChoNgoiId != null && x.LichKhoiHanhTour.TrangThai != "DaHuy",

                SoNguoi = x.HanhKhachDatTours.Count(h => x.TrangThai == "DaHuy" || h.NgayNha == null),
                SoNguoiLon = x.HanhKhachDatTours.Count(h => (x.TrangThai == "DaHuy" || h.NgayNha == null) && h.LoaiHanhKhach != "Trẻ nhỏ"),
                SoTreNho = x.HanhKhachDatTours.Count(h => (x.TrangThai == "DaHuy" || h.NgayNha == null) && h.LoaiHanhKhach == "Trẻ nhỏ"),
                TongTienTruocGiam = x.HanhKhachDatTours.Where(h => x.TrangThai == "DaHuy" || h.NgayNha == null).Sum(h => h.GiaLucDat),
                SoTienGiam = x.SoTienGiam,
                // Số phải trả không âm: tiền giảm không bao giờ vượt tổng giá các ghế
                TongTien = x.HanhKhachDatTours.Where(h => x.TrangThai == "DaHuy" || h.NgayNha == null).Sum(h => h.GiaLucDat) > x.SoTienGiam
                    ? x.HanhKhachDatTours.Where(h => x.TrangThai == "DaHuy" || h.NgayNha == null).Sum(h => h.GiaLucDat) - x.SoTienGiam
                    : 0,
                MaKhuyenMai = x.KhuyenMai != null ? x.KhuyenMai.MaKhuyenMai : null,

                // Phương thức của giao dịch gần nhất; đơn tiền mặt cũ chưa có giao dịch thì coi là tiền mặt
                PhuongThucThanhToan = db.GiaoDichThanhToans
                    .Where(g => g.DatTourId == x.Id)
                    .OrderByDescending(g => g.Id)
                    .Select(g => g.PhuongThuc)
                    .FirstOrDefault() ?? "Tiền mặt",
                TrangThaiThanhToan =
                    x.TrangThai == "DaHuy" ? TrangThaiThanhToanDon.DaHuy
                    : db.GiaoDichThanhToans.Any(g => g.DatTourId == x.Id && g.TrangThai == "ThanhCong") ? TrangThaiThanhToanDon.DaThanhToan
                    : db.GiaoDichThanhToans.Any(g => g.DatTourId == x.Id && g.TrangThai == "ChoXacNhan" && g.PhuongThuc != "Tiền mặt") ? TrangThaiThanhToanDon.DangKiemTra
                    : TrangThaiThanhToanDon.ChuaThanhToan
            });
        }
    }

    // Các thao tác ghi dùng chung cho một đơn (khách hủy ở Lịch sử, admin hủy ở trang đơn)
    public static class XuLyDon
    {
        // Hủy đơn: nhả mọi ghế chưa nhả, bỏ chỗ ngồi đã chọn, giao dịch còn chờ thành ThatBai,
        // trả đúng số ghế cho lịch, tất cả trong một giao dịch CSDL (RB-16, RB-17).
        // Giao dịch đã thành công giữ nguyên (tiền đã nhận, hoàn tiền xử lý ngoài hệ thống).
        // Đơn phải được nạp kèm HanhKhachDatTours.
        public static void HuyDon(DuLichDbContext db, DatTours don)
        {
            using var giaoDichCsdl = db.Database.BeginTransaction();

            var bayGio = DateTime.Now;
            var dsGhe = don.HanhKhachDatTours.Where(h => h.NgayNha == null).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            foreach (var ghe in dsGhe)
            {
                ghe.NgayNha = bayGio;
            }
            db.ChoNgoiChuyenDis.RemoveRange(db.ChoNgoiChuyenDis.Where(c => dsGheId.Contains(c.HanhKhachDatTourId)));

            foreach (var gd in db.GiaoDichThanhToans.Where(g => g.DatTourId == don.Id && g.TrangThai == "ChoXacNhan"))
            {
                gd.TrangThai = "ThatBai";
            }

            don.TrangThai = "DaHuy";
            db.SaveChanges();

            if (dsGhe.Count > 0)
            {
                db.Database.ExecuteSqlInterpolated(
                    $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon + {dsGhe.Count} WHERE Id = {don.LichKhoiHanhTourId} AND TrangThai <> {TrangThaiLich.DaHuy}");
            }

            giaoDichCsdl.Commit();
        }
    }
}
