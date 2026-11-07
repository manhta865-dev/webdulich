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
        public bool DuocDoiCho => !DaHuy && CoSoDoChoNgoi && NgayDi.Date > GioVN.HomNay;

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

        // Hạn giữ chỗ (đơn nhóm, và đơn cá nhân từ RB-38); đơn cũ có thể rỗng
        public DateTime? HanGiuCho { get; set; }

        // RB-39: khách tự hủy đơn cá nhân chỉ khi chưa trả, chưa báo chuyển khoản, và chưa tới ngày đi.
        // Đã trả hoặc đang chờ kiểm tra chuyển khoản thì liên hệ TravelVN (hoàn tiền xử lý ngoài hệ thống).
        public bool KhachTuHuyDuoc => LoaiDon != "Nhom" && TrangThai == "GiuCho"
            && TrangThaiThanhToan == TrangThaiThanhToanDon.ChuaThanhToan && NgayDi.Date > GioVN.HomNay;

        // RB-38: đơn cá nhân đã báo chuyển khoản, quá hạn giữ chỗ mà admin chưa xác nhận: không tự hủy, admin cần kiểm sao kê
        public bool QuaHanChuaXacNhan => LoaiDon != "Nhom" && TrangThai == "GiuCho"
            && HanGiuCho.HasValue && HanGiuCho.Value <= GioVN.BayGio;
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
                HanGiuCho = x.HanGiuCho,

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
                // Đơn nhóm tính theo ghế còn hiệu lực: mọi ghế đã có giao dịch thành công thì đã thanh toán,
                // có ghế đang chờ xác nhận thì đang kiểm tra (N4)
                TrangThaiThanhToan =
                    x.TrangThai == "DaHuy" ? TrangThaiThanhToanDon.DaHuy
                    : x.LoaiDon == "Nhom"
                        ? (x.HanhKhachDatTours.Any(h => h.NgayNha == null)
                           && x.HanhKhachDatTours.All(h => h.NgayNha != null || db.GiaoDichThanhToans.Any(g => g.HanhKhachDatTourId == h.Id && g.TrangThai == "ThanhCong"))
                            ? TrangThaiThanhToanDon.DaThanhToan
                            : db.GiaoDichThanhToans.Any(g => g.HanhKhachDatTour != null && g.HanhKhachDatTour.DatTourId == x.Id && g.HanhKhachDatTour.NgayNha == null && g.TrangThai == "ChoXacNhan")
                                ? TrangThaiThanhToanDon.DangKiemTra
                                : TrangThaiThanhToanDon.ChuaThanhToan)
                    : db.GiaoDichThanhToans.Any(g => g.DatTourId == x.Id && g.TrangThai == "ThanhCong") ? TrangThaiThanhToanDon.DaThanhToan
                    : db.GiaoDichThanhToans.Any(g => g.DatTourId == x.Id && g.TrangThai == "ChoXacNhan" && g.PhuongThuc != "Tiền mặt") ? TrangThaiThanhToanDon.DangKiemTra
                    : TrangThaiThanhToanDon.ChuaThanhToan
            });
        }
    }

    // Các thao tác ghi dùng chung cho một đơn (khách hủy ở Lịch sử, admin hủy ở trang đơn)
    public static class XuLyDon
    {
        // Mã chuyến đi dạng TV-XXXXXX: ngắn để làm nội dung chuyển khoản, bỏ các ký tự dễ nhầm (0, O, 1, I),
        // kiểm tra chưa có đơn nào dùng (lỗi 2).
        private const string KyTuMa = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string TaoMaChuyenDi(DuLichDbContext db)
        {
            for (int lan = 0; lan < 20; lan++)
            {
                var ma = "TV-" + new string(Enumerable.Range(0, 6)
                    .Select(_ => KyTuMa[System.Security.Cryptography.RandomNumberGenerator.GetInt32(KyTuMa.Length)])
                    .ToArray());
                if (!db.DatTours.Any(x => x.MaChuyenDi == ma))
                {
                    return ma;
                }
            }
            throw new InvalidOperationException("Không tạo được mã chuyến đi chưa dùng.");
        }

        // Mã mời của đơn nhóm: 24 ký tự ngẫu nhiên an toàn cho URL, khó đoán (RB-23)
        public static string TaoMaMoi(DuLichDbContext db)
        {
            for (int lan = 0; lan < 20; lan++)
            {
                var ma = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(18))
                    .Replace('+', '-').Replace('/', '_');
                if (!db.DatTours.Any(x => x.MaMoi == ma))
                {
                    return ma;
                }
            }
            throw new InvalidOperationException("Không tạo được mã mời chưa dùng.");
        }

        // Hạn giữ chỗ của đơn nhóm: sau số giờ cấu hình, nhưng muộn nhất 23:59:59 hôm trước ngày đi,
        // vì tới ngày đi đơn không nhận thay đổi nữa. Kết quả không sau bây giờ nghĩa là đã quá sát ngày, không tạo đơn được.
        public static DateTime TinhHanGiuCho(DateTime bayGio, int soGio, DateTime ngayKhoiHanh)
        {
            var muonNhat = ngayKhoiHanh.Date.AddSeconds(-1);
            var theoSoGio = bayGio.AddHours(soGio);
            return theoSoGio < muonNhat ? theoSoGio : muonNhat;
        }

        // Giá lúc đặt của một ghế theo loại hành khách
        public static decimal GiaGhe(string? loaiHanhKhach, decimal giaNguoiLon, decimal giaTreEm) =>
            loaiHanhKhach == "Trẻ nhỏ" ? giaTreEm : giaNguoiLon;

        // Hủy đơn: nhả mọi ghế chưa nhả, bỏ chỗ ngồi đã chọn, giao dịch còn chờ thành ThatBai,
        // trả đúng số ghế cho lịch, tất cả trong một giao dịch CSDL (RB-16, RB-17).
        // Giao dịch đã thành công giữ nguyên (tiền đã nhận, hoàn tiền xử lý ngoài hệ thống).
        // Đơn phải được nạp kèm HanhKhachDatTours.
        public static void HuyDon(DuLichDbContext db, DatTours don)
        {
            using var giaoDichCsdl = db.Database.BeginTransaction();

            var bayGio = GioVN.BayGio;
            var dsGhe = don.HanhKhachDatTours.Where(h => h.NgayNha == null).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            foreach (var ghe in dsGhe)
            {
                ghe.NgayNha = bayGio;
            }
            db.ChoNgoiChuyenDis.RemoveRange(db.ChoNgoiChuyenDis.Where(c => dsGheId.Contains(c.HanhKhachDatTourId)));

            // Giao dịch còn chờ của cả đơn (đơn cá nhân) và của từng ghế (đơn nhóm)
            foreach (var gd in db.GiaoDichThanhToans.Where(g => g.TrangThai == "ChoXacNhan"
                && (g.DatTourId == don.Id || (g.HanhKhachDatTourId != null && dsGheId.Contains(g.HanhKhachDatTourId.Value)))))
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

        // RB-38: đơn cá nhân quá hạn giữ chỗ mà chưa trả thì tự hủy, trả chỗ cho lịch.
        // Chỉ hủy đơn chọn tiền mặt (hoặc không có giao dịch): đơn đã báo chuyển khoản hay ví thì giữ lại để admin kiểm sao kê,
        // tránh khách đã chuyển tiền mất chỗ vì admin xác nhận chậm. Câu UPDATE đầu tiên có điều kiện "còn giữ chỗ, chưa có giao dịch
        // thành công, không có chuyển khoản đang chờ", nên admin xác nhận cùng lúc thì một trong hai bên không đổi gì.
        // Trả về true nếu đã hủy.
        public static bool HuyDonCaNhanQuaHan(DuLichDbContext db, int donId)
        {
            using var giaoDichCsdl = db.Database.BeginTransaction();
            var bayGio = GioVN.BayGio;
            const string ghiChu = "Quá hạn giữ chỗ chưa thanh toán, hệ thống tự hủy.";

            int soDong = db.Database.ExecuteSqlInterpolated($@"
UPDATE DatTours
SET TrangThai = 'DaHuy',
    GhiChu = CASE WHEN GhiChu IS NULL OR GhiChu = N'' THEN {ghiChu} ELSE GhiChu + NCHAR(10) + {ghiChu} END
WHERE Id = {donId} AND LoaiDon = 'CaNhan' AND TrangThai = 'GiuCho' AND HanGiuCho <= {bayGio}
  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans g WHERE g.DatTourId = {donId}
                  AND (g.TrangThai = 'ThanhCong' OR (g.TrangThai = 'ChoXacNhan' AND g.PhuongThuc <> N'Tiền mặt')))");
            if (soDong == 0)
            {
                giaoDichCsdl.Rollback();
                return false;
            }

            // Bỏ chỗ ngồi, nhả ghế, giao dịch tiền mặt đang chờ thành không hiệu lực (RB-16, RB-17)
            db.Database.ExecuteSqlInterpolated($@"
DELETE c FROM ChoNgoiChuyenDis c JOIN HanhKhachDatTours h ON h.Id = c.HanhKhachDatTourId
WHERE h.DatTourId = {donId} AND h.NgayNha IS NULL");
            int soNha = db.Database.ExecuteSqlInterpolated(
                $"UPDATE HanhKhachDatTours SET NgayNha = {bayGio} WHERE DatTourId = {donId} AND NgayNha IS NULL");
            db.Database.ExecuteSqlInterpolated(
                $"UPDATE GiaoDichThanhToans SET TrangThai = 'ThatBai' WHERE DatTourId = {donId} AND TrangThai = 'ChoXacNhan'");
            // Trả đúng số ghế vừa nhả (RB-29)
            if (soNha > 0)
            {
                db.Database.ExecuteSqlInterpolated($@"
UPDATE l SET SoChoCon = SoChoCon + {soNha}
FROM LichKhoiHanhTours l JOIN DatTours d ON d.LichKhoiHanhTourId = l.Id
WHERE d.Id = {donId} AND l.TrangThai <> {TrangThaiLich.DaHuy}");
            }

            giaoDichCsdl.Commit();
            return true;
        }

        // Chốt đơn nhóm: nhả mọi ghế chưa có giao dịch chờ hoặc thành công, bỏ chỗ ngồi của chúng,
        // trả đúng số ghế cho lịch, đơn thành DaXacNhan (link mời hết hiệu lực vì đơn không còn GiuCho).
        // Một giao dịch CSDL, các câu UPDATE có điều kiện nên không nhả nhầm ghế vừa được báo chuyển khoản.
        // Điều kiện được chốt (còn ghế chờ, chưa ai trả) do nơi gọi kiểm. Trả về số ghế đã nhả, -1 nếu đơn không còn giữ chỗ.
        public static int ChotDonNhom(DuLichDbContext db, int donId, int lichId)
        {
            using var giaoDichCsdl = db.Database.BeginTransaction();

            int daChot =db.Database.ExecuteSqlInterpolated(
                $"UPDATE DatTours SET TrangThai = 'DaXacNhan' WHERE Id = {donId} AND LoaiDon = 'Nhom' AND TrangThai = 'GiuCho'");
            if (daChot == 0)
            {
                giaoDichCsdl.Rollback();
                return -1;
            }

            int soNha = NhaGheChuaTra(db, donId, lichId);
            giaoDichCsdl.Commit();
            return soNha;
        }

        // Nhả mọi ghế còn hiệu lực chưa có giao dịch chờ hoặc thành công (RB-28), bỏ chỗ ngồi của ghế đã nhả,
        // trả đúng số ghế vừa nhả cho lịch (RB-29). Chạy bên trong giao dịch CSDL của nơi gọi. Trả về số ghế đã nhả.
        private static int NhaGheChuaTra(DuLichDbContext db, int donId, int lichId)
        {
            int soNha = db.Database.ExecuteSqlInterpolated($@"
                UPDATE HanhKhachDatTours SET NgayNha = {GioVN.BayGio}
                WHERE DatTourId = {donId} AND NgayNha IS NULL
                  AND NOT EXISTS (SELECT 1 FROM GiaoDichThanhToans g WHERE g.HanhKhachDatTourId = HanhKhachDatTours.Id AND g.TrangThai IN ('ChoXacNhan', 'ThanhCong'))");

            db.Database.ExecuteSqlInterpolated($@"
                DELETE FROM ChoNgoiChuyenDis
                WHERE HanhKhachDatTourId IN (SELECT Id FROM HanhKhachDatTours WHERE DatTourId = {donId} AND NgayNha IS NOT NULL)");

            if (soNha > 0)
            {
                db.Database.ExecuteSqlInterpolated(
                    $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon + {soNha} WHERE Id = {lichId} AND TrangThai <> {TrangThaiLich.DaHuy}");
            }
            return soNha;
        }

        // ---------- Hết hạn giữ chỗ của đơn nhóm (N5) ----------

        public enum KetQuaHetHan { KhongDoi, GiaHan, NhaGheChoAdmin, Chot, Huy }

        // Xử lý một đơn nhóm đã quá hạn giữ chỗ, theo luật chốt ngày 30/10 và 31/10/2026 (docs/MO_HINH_DU_LIEU.md, mục 9):
        //   mọi ghế đã trả -> chốt;
        //   còn ghế chưa trả, chưa gia hạn, còn gia hạn được -> gia hạn một lần (không vượt 23:59:59 hôm trước ngày đi);
        //   hết lượt gia hạn -> nhả ghế chưa trả; còn ghế chờ admin thì đợi, không thì chốt (có ghế đã trả) hoặc hủy (không ai trả).
        // Dịch vụ chạy nền và lúc mở trang cùng gọi; mỗi bước là UPDATE có điều kiện nên hai nơi gọi cùng lúc chỉ một nơi làm được.
        public static KetQuaHetHan XuLyDonQuaHan(DuLichDbContext db, int donId, int soGioGiaHan)
        {
            var bayGio = GioVN.BayGio;
            var don = db.DatTours.AsNoTracking().Include(x => x.LichKhoiHanhTour)
                .FirstOrDefault(x => x.Id == donId && x.LoaiDon == "Nhom" && x.TrangThai == "GiuCho");
            if (don?.LichKhoiHanhTour == null || !(don.HanGiuCho <= bayGio))
            {
                return KetQuaHetHan.KhongDoi;
            }

            var dsGhe = db.HanhKhachDatTours.AsNoTracking().Where(h => h.DatTourId == donId && h.NgayNha == null).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            var dsGiaoDich = db.GiaoDichThanhToans.AsNoTracking()
                .Where(g => g.HanhKhachDatTourId != null && dsGheId.Contains(g.HanhKhachDatTourId.Value)).ToList();
            var dsTrangThai = dsGhe.Select(h => TrangThaiGhe.Tinh(h, dsGiaoDich)).ToList();
            int soDaTra = dsTrangThai.Count(t => t == TrangThaiGhe.DaTra);
            int soCho = dsTrangThai.Count(t => t == TrangThaiGhe.ChoXacNhan);
            int soChuaTra = dsTrangThai.Count - soDaTra - soCho;

            // Còn người chưa trả và chưa dùng lượt gia hạn: gia hạn tính từ bây giờ (lần quét có thể trễ vài phút)
            if (soChuaTra > 0 && !don.DaGiaHan)
            {
                var hanMoi = TinhHanGiuCho(bayGio, soGioGiaHan, don.LichKhoiHanhTour.NgayKhoiHanh);
                if (hanMoi > bayGio)
                {
                    int daGiaHan = db.Database.ExecuteSqlInterpolated(
                        $"UPDATE DatTours SET HanGiuCho = {hanMoi}, DaGiaHan = 1 WHERE Id = {donId} AND TrangThai = 'GiuCho' AND DaGiaHan = 0");
                    return daGiaHan > 0 ? KetQuaHetHan.GiaHan : KetQuaHetHan.KhongDoi;
                }
                // Đã chạm mốc trước ngày đi: không gia hạn được, xử lý như hết lượt
            }

            if (soCho > 0)
            {
                // Không nhả ghế đang chờ admin (RB-22); nhả phần chưa trả ngay để trả chỗ cho lịch, đơn đợi admin
                if (soChuaTra == 0)
                {
                    return KetQuaHetHan.KhongDoi;
                }
                using var giaoDichCsdl = db.Database.BeginTransaction();
                // Khóa đơn: chỉ nhả khi đơn vẫn đang giữ chỗ (nơi gọi khác có thể vừa chốt hoặc hủy)
                int conGiuCho = db.Database.ExecuteSqlInterpolated(
                    $"UPDATE DatTours SET TrangThai = TrangThai WHERE Id = {donId} AND TrangThai = 'GiuCho'");
                int soNha = conGiuCho > 0 ? NhaGheChuaTra(db, donId, don.LichKhoiHanhTourId) : 0;
                giaoDichCsdl.Commit();
                return soNha > 0 ? KetQuaHetHan.NhaGheChoAdmin : KetQuaHetHan.KhongDoi;
            }

            if (soDaTra > 0)
            {
                return ChotDonNhom(db, donId, don.LichKhoiHanhTourId) >= 0 ? KetQuaHetHan.Chot : KetQuaHetHan.KhongDoi;
            }

            return HuyDonNhomQuaHan(db, donId, don.LichKhoiHanhTourId) ? KetQuaHetHan.Huy : KetQuaHetHan.KhongDoi;
        }

        // Hủy đơn nhóm không ai trả khi hết hạn: giống HuyDon nhưng có điều kiện "đơn vẫn GiuCho"
        // để dịch vụ nền và lúc mở trang không cùng hủy rồi cộng chỗ cho lịch hai lần.
        private static bool HuyDonNhomQuaHan(DuLichDbContext db, int donId, int lichId)
        {
            using var giaoDichCsdl = db.Database.BeginTransaction();
            int daHuy = db.Database.ExecuteSqlInterpolated(
                $"UPDATE DatTours SET TrangThai = 'DaHuy' WHERE Id = {donId} AND LoaiDon = 'Nhom' AND TrangThai = 'GiuCho'");
            if (daHuy == 0)
            {
                giaoDichCsdl.Rollback();
                return false;
            }

            int soNha = db.Database.ExecuteSqlInterpolated(
                $"UPDATE HanhKhachDatTours SET NgayNha = {GioVN.BayGio} WHERE DatTourId = {donId} AND NgayNha IS NULL");
            db.Database.ExecuteSqlInterpolated($@"
                DELETE FROM ChoNgoiChuyenDis WHERE HanhKhachDatTourId IN (SELECT Id FROM HanhKhachDatTours WHERE DatTourId = {donId})");
            db.Database.ExecuteSqlInterpolated($@"
                UPDATE GiaoDichThanhToans SET TrangThai = 'ThatBai'
                WHERE TrangThai = 'ChoXacNhan' AND HanhKhachDatTourId IN (SELECT Id FROM HanhKhachDatTours WHERE DatTourId = {donId})");
            if (soNha > 0)
            {
                db.Database.ExecuteSqlInterpolated(
                    $"UPDATE LichKhoiHanhTours SET SoChoCon = SoChoCon + {soNha} WHERE Id = {lichId} AND TrangThai <> {TrangThaiLich.DaHuy}");
            }
            giaoDichCsdl.Commit();
            return true;
        }
    }
}
