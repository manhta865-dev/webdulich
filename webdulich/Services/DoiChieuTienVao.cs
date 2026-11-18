using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Services
{
    // Một khoản tiền vào do dịch vụ báo về (hoặc trang giả lập tạo ra), đã đọc ra các trường cần dùng
    public record TienVao(string Nguon, string MaGiaoDichNguon, string? NganHang, string? SoTaiKhoan,
        DateTime ThoiGian, decimal SoTien, string NoiDung, string? MaThamChieu);

    // RB-48: tự xác nhận chuyển khoản. Mỗi khoản tiền vào được ghi vào GiaoDichNganHangs (UNIQUE nguồn + mã giao dịch
    // nên báo trùng chỉ tính một lần), rồi đối chiếu nội dung với mã ghế (đơn nhóm, TV-XXXXXX-n) hoặc mã đơn (đơn cá nhân, TV-XXXXXX).
    // Chỉ tự xác nhận khi số tiền khớp đúng và ghế/đơn còn hiệu lực, chưa trả; mọi trường hợp khác giữ nguyên để admin xử lý tay.
    // Ghi khoản tiền vào, giao dịch thanh toán và trạng thái đơn trong một giao dịch CSDL.
    public static class DoiChieuTienVao
    {
        public const string PhuongThucTuDong = "Chuyển khoản (tự động)";

        // Mã chuyến đi: "TV" + 6 ký tự trong bảng chữ của XuLyDon.TaoMaChuyenDi (không có 0, O, 1, I), có thể kèm số ghế
        private static readonly Regex MaTrongNoiDung = new("TV([A-HJ-NP-Z2-9]{6})([0-9]{0,2})", RegexOptions.Compiled);

        public record KetQuaNhan(GiaoDichNganHang Dong, bool LaTrung);

        public static KetQuaNhan Nhan(DuLichDbContext db, TienVao tv)
        {
            var daCo = db.GiaoDichNganHangs.AsNoTracking()
                .FirstOrDefault(x => x.Nguon == tv.Nguon && x.MaGiaoDichNguon == tv.MaGiaoDichNguon);
            if (daCo != null)
            {
                return new KetQuaNhan(daCo, true);
            }

            using var giaoDichCsdl = db.Database.BeginTransaction();
            var dong = new GiaoDichNganHang
            {
                Nguon = tv.Nguon,
                MaGiaoDichNguon = Cat(tv.MaGiaoDichNguon, 64),
                NganHang = CatRong(tv.NganHang, 50),
                SoTaiKhoan = CatRong(tv.SoTaiKhoan, 30),
                ThoiGianGiaoDich = tv.ThoiGian,
                SoTien = tv.SoTien,
                NoiDung = Cat(tv.NoiDung, 500),
                MaThamChieu = CatRong(tv.MaThamChieu, 100),
                NgayNhan = GioVN.BayGio,
                KetQua = KetQuaTienVao.KhongTimThayMa
            };
            db.GiaoDichNganHangs.Add(dong);
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException loi) when (loi.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Cùng giao dịch vừa được ghi bởi lần báo khác
                giaoDichCsdl.Rollback();
                db.ChangeTracker.Clear();
                var cu = db.GiaoDichNganHangs.AsNoTracking().First(x => x.Nguon == tv.Nguon && x.MaGiaoDichNguon == tv.MaGiaoDichNguon);
                return new KetQuaNhan(cu, true);
            }

            DoiChieu(db, dong);
            db.SaveChanges();
            giaoDichCsdl.Commit();
            return new KetQuaNhan(dong, false);
        }

        // Đơn cá nhân vừa tạo: khách có thể đã chuyển khoản trước khi bấm xác nhận, lúc đó chưa có đơn nên khoản tiền
        // bị ghi "không tìm thấy mã". Đối chiếu lại các khoản đó (chưa ai xử lý) có chứa mã của đơn này.
        public static int DoiChieuLaiChoMa(DuLichDbContext db, string maChuyenDi)
        {
            var ma = ChuanHoa(maChuyenDi);
            if (ma.Length < 8)
            {
                return 0;
            }
            var dsCho = db.GiaoDichNganHangs
                .Where(x => x.KetQua == KetQuaTienVao.KhongTimThayMa && x.NguoiXuLyId == null)
                .OrderBy(x => x.Id)
                .ToList()
                .Where(x => ChuanHoa(x.NoiDung).Contains(ma))
                .ToList();

            int soKhop = 0;
            foreach (var dong in dsCho)
            {
                using var giaoDichCsdl = db.Database.BeginTransaction();
                DoiChieu(db, dong);
                db.SaveChanges();
                giaoDichCsdl.Commit();
                if (dong.KetQua == KetQuaTienVao.DaKhop)
                {
                    soKhop++;
                }
            }
            return soKhop;
        }

        // Viết hoa, bỏ mọi ký tự không phải chữ cái Latinh và số: ngân hàng hay bỏ dấu gạch, thêm dấu cách hoặc tên người chuyển
        public static string ChuanHoa(string? s) =>
            Regex.Replace((s ?? string.Empty).ToUpperInvariant(), "[^A-Z0-9]", string.Empty);

        // Đối chiếu một khoản (đã được theo dõi trong db) và cập nhật KetQua, GiaoDichThanhToanId.
        // Nơi gọi mở giao dịch CSDL và SaveChanges.
        private static void DoiChieu(DuLichDbContext db, GiaoDichNganHang dong)
        {
            var noiDung = ChuanHoa(dong.NoiDung);
            foreach (Match m in MaTrongNoiDung.Matches(noiDung))
            {
                string maDon = "TV-" + m.Groups[1].Value;
                string so = m.Groups[2].Value;

                // Mã ghế trước (thử 2 chữ số rồi 1 chữ số sau mã đơn), không có thì mã đơn cá nhân
                for (int dai = so.Length; dai >= 1; dai--)
                {
                    var maGhe = maDon + "-" + so.Substring(0, dai);
                    var ghe = db.HanhKhachDatTours.Include(h => h.DatTour)
                        .FirstOrDefault(h => h.MaThanhToan == maGhe);
                    if (ghe?.DatTour != null)
                    {
                        XuLyGhe(db, dong, ghe);
                        return;
                    }
                }

                var don = db.DatTours.FirstOrDefault(d => d.MaChuyenDi == maDon && d.LoaiDon == "CaNhan");
                if (don != null)
                {
                    XuLyDonCaNhan(db, dong, don);
                    return;
                }
            }
            dong.KetQua = KetQuaTienVao.KhongTimThayMa;
        }

        private static void XuLyGhe(DuLichDbContext db, GiaoDichNganHang dong, HanhKhachDatTour ghe)
        {
            if (ghe.DatTour!.TrangThai == "DaHuy")
            {
                dong.KetQua = KetQuaTienVao.DonDaHuy;
                return;
            }
            if (ghe.NgayNha.HasValue)
            {
                dong.KetQua = KetQuaTienVao.GheDaNha;
                return;
            }
            if (db.GiaoDichThanhToans.Any(g => g.HanhKhachDatTourId == ghe.Id && g.TrangThai == "ThanhCong"))
            {
                dong.KetQua = KetQuaTienVao.DaTraRoi;
                return;
            }
            if (dong.SoTien != ghe.SoTienPhaiTra)
            {
                dong.KetQua = KetQuaTienVao.SaiSoTien;
                return;
            }

            var gdCho = db.GiaoDichThanhToans
                .Where(g => g.HanhKhachDatTourId == ghe.Id && g.TrangThai == "ChoXacNhan")
                .OrderByDescending(g => g.Id).FirstOrDefault();
            var gd = gdCho ?? new GiaoDichThanhToan
            {
                HanhKhachDatTourId = ghe.Id,
                NguoiTraId = ghe.TaiKhoanId,
                NgayTao = GioVN.BayGio
            };
            GhiThanhCong(db, dong, gd);
        }

        private static void XuLyDonCaNhan(DuLichDbContext db, GiaoDichNganHang dong, DatTours don)
        {
            if (don.TrangThai == "DaHuy")
            {
                dong.KetQua = KetQuaTienVao.DonDaHuy;
                return;
            }
            if (db.GiaoDichThanhToans.Any(g => g.DatTourId == don.Id && g.TrangThai == "ThanhCong"))
            {
                dong.KetQua = KetQuaTienVao.DaTraRoi;
                return;
            }
            // Số phải trả tính từ ghế và tiền giảm (không lưu cột), giống trang thanh toán
            var tongTien = db.DatTours.AsNoTracking().Where(d => d.Id == don.Id).ChonDon(db).Select(d => d.TongTien).First();
            if (dong.SoTien != tongTien)
            {
                dong.KetQua = KetQuaTienVao.SaiSoTien;
                return;
            }

            var gdCho = db.GiaoDichThanhToans
                .Where(g => g.DatTourId == don.Id && g.TrangThai == "ChoXacNhan")
                .OrderByDescending(g => g.Id).FirstOrDefault();
            var gd = gdCho ?? new GiaoDichThanhToan
            {
                DatTourId = don.Id,
                NguoiTraId = don.TaiKhoanId,
                NgayTao = GioVN.BayGio
            };
            if (!GhiThanhCong(db, dong, gd))
            {
                return;
            }
            // Như admin bấm "Xác nhận đã nhận tiền": đơn đang giữ chỗ thành đã xác nhận
            if (don.TrangThai == "GiuCho")
            {
                don.TrangThai = "DaXacNhan";
            }
        }

        // Giao dịch thành công, người xác nhận để trống nghĩa là hệ thống. Chỉ mục duy nhất có lọc của GiaoDichThanhToans
        // vẫn chặn một ghế/đơn có hai giao dịch thành công (RB-05); bị chặn thì coi là đã trả trước đó.
        private static bool GhiThanhCong(DuLichDbContext db, GiaoDichNganHang dong, GiaoDichThanhToan gd)
        {
            var bayGio = GioVN.BayGio;
            gd.TrangThai = "ThanhCong";
            gd.SoTien = dong.SoTien;
            gd.PhuongThuc = PhuongThucTuDong;
            gd.NgayXacNhan = bayGio;
            gd.NguoiXacNhanId = null;
            gd.MaThamChieuNganHang = dong.MaThamChieu ?? dong.MaGiaoDichNguon;
            if (gd.Id == 0)
            {
                db.GiaoDichThanhToans.Add(gd);
            }
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException loi) when (loi.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Bỏ thay đổi của giao dịch thanh toán, giữ dòng tiền vào để ghi kết quả
                foreach (var muc in db.ChangeTracker.Entries().Where(e => e.Entity is GiaoDichThanhToan).ToList())
                {
                    if (muc.State == EntityState.Added) muc.State = EntityState.Detached;
                    else muc.Reload();
                }
                dong.KetQua = KetQuaTienVao.DaTraRoi;
                return false;
            }
            dong.KetQua = KetQuaTienVao.DaKhop;
            dong.GiaoDichThanhToanId = gd.Id;
            return true;
        }

        private static string Cat(string? s, int toiDa) => (s ?? string.Empty).Length > toiDa ? s![..toiDa] : s ?? string.Empty;

        private static string? CatRong(string? s, int toiDa) => string.IsNullOrWhiteSpace(s) ? null : Cat(s.Trim(), toiDa);
    }
}
