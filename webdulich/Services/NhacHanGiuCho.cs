using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Services
{
    // RB-47: nhắc qua email khi đơn nhóm còn giữ chỗ, hạn còn dưới DonNhom:SoGioNhacTruocHan giờ, và còn ghế chưa trả
    // (chưa có người nhận, đã nhận, đã điền; ghế đang chờ admin xác nhận coi như đã chuyển tiền nên không nhắc).
    //   - Thành viên có ghế chưa trả: một thư riêng cho ghế của mình.
    //   - Trưởng nhóm: một thư tóm tắt mọi ghế chưa trả (kể cả ghế của chính mình), chỉ có tên và trạng thái, không có giấy tờ (RB-20).
    // Mỗi người một lần cho mỗi mốc hạn: UNIQUE (DatTourId, TaiKhoanId, HanDuocNhac). Gia hạn đổi mốc nên nhắc thêm một lần.
    // Ghi dòng NhacThanhToans trước rồi mới gửi, nên hai lần quét cùng lúc chỉ một lần gửi được. Gửi lỗi thì lần quét sau thử lại,
    // tối đa TrangThaiNhac.SoLanThuToiDa lần. Người chưa có email thì bỏ qua.
    // Dịch vụ nền gọi sau bước xử lý hết hạn (XuLyHetHanDonNhom), nên đơn vừa được gia hạn cũng được nhắc ngay ở lần quét đó.
    public static class NhacHanGiuCho
    {
        private static readonly CultureInfo VN = CultureInfo.GetCultureInfo("vi-VN");

        // Trả về số thư đã gửi được trong lần quét
        public static int ChayMotLan(DuLichDbContext db, IGuiEmail guiEmail, IConfiguration cauHinh, ILogger nhatKy)
        {
            if (!guiEmail.DangBat)
            {
                return 0;
            }

            int soGioNhac = Math.Max(1, cauHinh.GetValue("DonNhom:SoGioNhacTruocHan", 6));
            string diaChiWeb = (cauHinh["Email:DiaChiWeb"] ?? "").TrimEnd('/');
            var bayGio = GioVN.BayGio;
            var mocNhac = bayGio.AddHours(soGioNhac);

            var dsDon = db.DatTours.AsNoTracking()
                .Include(x => x.LichKhoiHanhTour).ThenInclude(l => l!.Tour)
                .Include(x => x.TaiKhoan)
                .Where(x => x.LoaiDon == "Nhom" && x.TrangThai == "GiuCho"
                    && x.HanGiuCho > bayGio && x.HanGiuCho <= mocNhac)
                .ToList();

            int soDaGui = 0;
            foreach (var don in dsDon)
            {
                try
                {
                    soDaGui += NhacMotDon(db, guiEmail, don, diaChiWeb, nhatKy);
                }
                catch (Exception loi)
                {
                    db.ChangeTracker.Clear();
                    nhatKy.LogError(loi, "Lỗi khi nhắc thanh toán đơn nhóm {DonId}", don.Id);
                }
            }
            return soDaGui;
        }

        private static int NhacMotDon(DuLichDbContext db, IGuiEmail guiEmail, DatTours don, string diaChiWeb, ILogger nhatKy)
        {
            var han = don.HanGiuCho!.Value;
            // Mọi ghế của đơn theo Id, để đánh số giống trang đơn nhóm (Ghế 1, Ghế 2...)
            var dsGhe = db.HanhKhachDatTours.AsNoTracking().Include(h => h.TaiKhoan)
                .Where(h => h.DatTourId == don.Id).OrderBy(h => h.Id).ToList();
            var dsGheId = dsGhe.Select(h => h.Id).ToList();
            var dsGiaoDich = db.GiaoDichThanhToans.AsNoTracking()
                .Where(g => g.HanhKhachDatTourId != null && dsGheId.Contains(g.HanhKhachDatTourId.Value)).ToList();

            var dsChuaTra = dsGhe
                .Select((h, i) => new GheCanNhac(h, i + 1, TrangThaiGhe.Tinh(h, dsGiaoDich)))
                .Where(g => g.TrangThai is TrangThaiGhe.ChuaNhan or TrangThaiGhe.DaNhan or TrangThaiGhe.DaDien)
                .ToList();
            if (dsChuaTra.Count == 0)
            {
                return 0;
            }

            int soDaGui = 0;

            // Thành viên (không phải trưởng nhóm) có ghế chưa trả
            foreach (var g in dsChuaTra.Where(g => g.Ghe.TaiKhoanId.HasValue && g.Ghe.TaiKhoanId != don.TaiKhoanId))
            {
                var tk = g.Ghe.TaiKhoan!;
                if (GuiMotNguoi(db, guiEmail, don, tk, han, () => ThuThanhVien(don, tk, g, diaChiWeb), nhatKy))
                {
                    soDaGui++;
                }
            }

            // Trưởng nhóm: một thư tóm tắt
            if (don.TaiKhoan != null)
            {
                var tk = don.TaiKhoan;
                if (GuiMotNguoi(db, guiEmail, don, tk, han, () => ThuTruongNhom(don, tk, dsChuaTra, diaChiWeb), nhatKy))
                {
                    soDaGui++;
                }
            }
            return soDaGui;
        }

        private record GheCanNhac(HanhKhachDatTour Ghe, int ThuTu, string TrangThai);

        // Giành lượt nhắc của một người cho mốc hạn này rồi gửi. Trả về true nếu gửi được.
        private static bool GuiMotNguoi(DuLichDbContext db, IGuiEmail guiEmail, DatTours don, TaiKhoan tk, DateTime han,
            Func<ThuEmail> soanThu, ILogger nhatKy)
        {
            var email = tk.Email?.Trim();
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            {
                return false;
            }

            var nhac = db.NhacThanhToans.FirstOrDefault(x => x.DatTourId == don.Id && x.TaiKhoanId == tk.Id && x.HanDuocNhac == han);
            if (nhac == null)
            {
                nhac = new NhacThanhToan
                {
                    DatTourId = don.Id,
                    TaiKhoanId = tk.Id,
                    HanDuocNhac = han,
                    EmailNhan = email.Length > 256 ? email[..256] : email,
                    NgayTao = GioVN.BayGio,
                    TrangThai = TrangThaiNhac.ChoGui,
                    SoLanThu = 1
                };
                db.NhacThanhToans.Add(nhac);
                try
                {
                    db.SaveChanges();
                }
                catch (DbUpdateException loi) when (loi.InnerException is SqlException { Number: 2601 or 2627 })
                {
                    // Lần quét khác vừa giành lượt này
                    db.ChangeTracker.Clear();
                    return false;
                }
            }
            else if (nhac.TrangThai == TrangThaiNhac.Loi && nhac.SoLanThu < TrangThaiNhac.SoLanThuToiDa)
            {
                // Thử lại lần gửi lỗi trước; UPDATE có điều kiện nên chỉ một nơi thử được
                int daGianh = db.Database.ExecuteSqlInterpolated($@"
                    UPDATE NhacThanhToans SET TrangThai = 'ChoGui', SoLanThu = SoLanThu + 1
                    WHERE Id = {nhac.Id} AND TrangThai = 'Loi' AND SoLanThu < {TrangThaiNhac.SoLanThuToiDa}");
                db.Entry(nhac).Reload();
                if (daGianh == 0)
                {
                    return false;
                }
            }
            else
            {
                // Đã gửi, đang gửi, hoặc hết lượt thử
                return false;
            }

            try
            {
                guiEmail.Gui(soanThu() with { Den = nhac.EmailNhan });
                nhac.TrangThai = TrangThaiNhac.DaGui;
                nhac.NgayGui = GioVN.BayGio;
                nhac.Loi = null;
                db.SaveChanges();
                return true;
            }
            catch (Exception loi)
            {
                var lyDo = loi.GetBaseException().Message;
                nhac.TrangThai = TrangThaiNhac.Loi;
                nhac.NgayGui = null;
                nhac.Loi = lyDo.Length > 500 ? lyDo[..500] : lyDo;
                db.SaveChanges();
                nhatKy.LogWarning("Gửi thư nhắc đơn {DonId} cho tài khoản {TaiKhoanId} lỗi lần {Lan}: {Loi}", don.Id, tk.Id, nhac.SoLanThu, lyDo);
                return false;
            }
        }

        // ---------- Soạn thư ----------

        private static string Tien(decimal soTien) => soTien.ToString("N0", VN) + " VNĐ";

        private static string Gio(DateTime t) => t.ToString("HH:mm 'ngày' dd/MM/yyyy", VN);

        private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

        private static string ConLai(DatTours don)
        {
            var conLai = don.HanGiuCho!.Value - GioVN.BayGio;
            if (conLai.TotalMinutes < 60)
            {
                return $"{Math.Max(1, (int)conLai.TotalMinutes)} phút";
            }
            return $"{(int)conLai.TotalHours} giờ" + (conLai.Minutes >= 5 ? $" {conLai.Minutes} phút" : "");
        }

        private static string ViecConThieu(GheCanNhac g) => g.TrangThai switch
        {
            TrangThaiGhe.DaDien => "chuyển khoản phần của ghế",
            TrangThaiGhe.DaNhan => "điền thông tin hành khách rồi chuyển khoản",
            _ => "chưa có người nhận ghế"
        };

        private static string TenTour(DatTours don) => don.LichKhoiHanhTour?.Tour?.TenTour?.Trim() ?? "tour";

        private static string NgayDi(DatTours don) => don.LichKhoiHanhTour?.NgayKhoiHanh.ToString("dd/MM/yyyy", VN) ?? "";

        private static string CauHetHan(DatTours don) => don.DaGiaHan
            ? "Đơn đã được gia hạn một lần. Quá hạn này, ghế nào chưa trả sẽ bị nhả và có thể không còn chỗ."
            : "Quá hạn mà nhóm vẫn còn người chưa trả, đơn được gia hạn thêm một lần ngắn; sau đó ghế chưa trả sẽ bị nhả.";

        private static ThuEmail ThuThanhVien(DatTours don, TaiKhoan tk, GheCanNhac g, string diaChiWeb)
        {
            string link = $"{diaChiWeb}/DonNhom/GheCuaToi/{g.Ghe.Id}";
            string tieuDe = $"[TravelVN] Còn {ConLai(don)} để trả phần của bạn - {TenTour(don)}";
            string viec = ViecConThieu(g);
            string truongNhom = don.TaiKhoan?.HoTen ?? don.TenKhach;

            var chu = new StringBuilder()
                .AppendLine($"Chào {tk.HoTen},")
                .AppendLine()
                .AppendLine($"Ghế {g.ThuTu} của bạn trong đơn nhóm {don.MaChuyenDi} (trưởng nhóm {truongNhom}), tour {TenTour(don)}, khởi hành {NgayDi(don)}, chưa được thanh toán.")
                .AppendLine($"Hạn giữ chỗ: {Gio(don.HanGiuCho!.Value)} (còn khoảng {ConLai(don)}).")
                .AppendLine($"Việc còn lại: {viec}.")
                .AppendLine($"Số tiền của ghế: {Tien(g.Ghe.SoTienPhaiTra)}. Nội dung chuyển khoản: {g.Ghe.MaThanhToan}.")
                .AppendLine(CauHetHan(don))
                .AppendLine()
                .AppendLine($"Mở trang ghế của bạn: {link}")
                .AppendLine()
                .AppendLine("Thư này gửi tự động từ TravelVN, bạn không cần trả lời.")
                .ToString();

            var html = $@"<div style=""font-family:Arial,sans-serif;font-size:15px;line-height:1.5;color:#1f2933"">
<p>Chào {E(tk.HoTen)},</p>
<p>Ghế {g.ThuTu} của bạn trong đơn nhóm <b>{E(don.MaChuyenDi)}</b> (trưởng nhóm {E(truongNhom)}), tour <b>{E(TenTour(don))}</b>, khởi hành {E(NgayDi(don))}, <b>chưa được thanh toán</b>.</p>
<table style=""border-collapse:collapse"">
<tr><td style=""padding:4px 12px 4px 0;color:#52606d"">Hạn giữ chỗ</td><td style=""padding:4px 0""><b>{E(Gio(don.HanGiuCho!.Value))}</b> (còn khoảng {E(ConLai(don))})</td></tr>
<tr><td style=""padding:4px 12px 4px 0;color:#52606d"">Việc còn lại</td><td style=""padding:4px 0"">{E(viec)}</td></tr>
<tr><td style=""padding:4px 12px 4px 0;color:#52606d"">Số tiền</td><td style=""padding:4px 0"">{E(Tien(g.Ghe.SoTienPhaiTra))}</td></tr>
<tr><td style=""padding:4px 12px 4px 0;color:#52606d"">Nội dung chuyển khoản</td><td style=""padding:4px 0""><b>{E(g.Ghe.MaThanhToan)}</b></td></tr>
</table>
<p>{E(CauHetHan(don))}</p>
<p><a href=""{E(link)}"" style=""display:inline-block;padding:10px 18px;background:#0f766e;color:#fff;text-decoration:none;border-radius:6px"">Mở trang ghế của bạn</a></p>
<p style=""color:#7b8794;font-size:13px"">Thư này gửi tự động từ TravelVN, bạn không cần trả lời.</p>
</div>";

            return new ThuEmail(tk.Email ?? "", tk.HoTen, tieuDe, html, chu);
        }

        private static ThuEmail ThuTruongNhom(DatTours don, TaiKhoan tk, List<GheCanNhac> dsChuaTra, string diaChiWeb)
        {
            string linkDon = $"{diaChiWeb}/DonNhom/ChiTiet/{don.Id}";
            string? linkMoi = string.IsNullOrEmpty(don.MaMoi) ? null : $"{diaChiWeb}/DonNhom/Moi?ma={Uri.EscapeDataString(don.MaMoi)}";
            string tieuDe = $"[TravelVN] Đơn nhóm {don.MaChuyenDi}: còn {dsChuaTra.Count} ghế chưa trả, hạn {Gio(don.HanGiuCho!.Value)}";

            // Chỉ tên và trạng thái, không có giấy tờ của người khác (RB-20)
            string Ten(GheCanNhac g) =>
                g.Ghe.TaiKhoanId == tk.Id ? "Ghế của bạn"
                : !string.IsNullOrWhiteSpace(g.Ghe.HoTen) ? g.Ghe.HoTen!
                : g.Ghe.TaiKhoan?.HoTen ?? "Chưa có người nhận";

            var chu = new StringBuilder()
                .AppendLine($"Chào {tk.HoTen},")
                .AppendLine()
                .AppendLine($"Đơn nhóm {don.MaChuyenDi}, tour {TenTour(don)}, khởi hành {NgayDi(don)}, còn {dsChuaTra.Count} ghế chưa trả.")
                .AppendLine($"Hạn giữ chỗ: {Gio(don.HanGiuCho!.Value)} (còn khoảng {ConLai(don)}).")
                .AppendLine();
            foreach (var g in dsChuaTra)
            {
                chu.AppendLine($"- Ghế {g.ThuTu}: {Ten(g)} - {ViecConThieu(g)} ({Tien(g.Ghe.SoTienPhaiTra)})");
            }
            chu.AppendLine()
                .AppendLine(CauHetHan(don))
                .AppendLine("Thành viên đã nhận ghế và có email cũng được TravelVN nhắc riêng.")
                .AppendLine()
                .AppendLine($"Trang quản lý đơn: {linkDon}");
            if (linkMoi != null)
            {
                chu.AppendLine($"Link mời gửi lại cho nhóm: {linkMoi}");
            }
            chu.AppendLine().AppendLine("Thư này gửi tự động từ TravelVN, bạn không cần trả lời.");

            var dong = new StringBuilder();
            foreach (var g in dsChuaTra)
            {
                dong.Append($@"<tr><td style=""padding:6px 10px;border-bottom:1px solid #e4e7eb"">Ghế {g.ThuTu}</td><td style=""padding:6px 10px;border-bottom:1px solid #e4e7eb"">{E(Ten(g))}</td><td style=""padding:6px 10px;border-bottom:1px solid #e4e7eb"">{E(ViecConThieu(g))}</td><td style=""padding:6px 10px;border-bottom:1px solid #e4e7eb;text-align:right"">{E(Tien(g.Ghe.SoTienPhaiTra))}</td></tr>");
            }

            var html = $@"<div style=""font-family:Arial,sans-serif;font-size:15px;line-height:1.5;color:#1f2933"">
<p>Chào {E(tk.HoTen)},</p>
<p>Đơn nhóm <b>{E(don.MaChuyenDi)}</b>, tour <b>{E(TenTour(don))}</b>, khởi hành {E(NgayDi(don))}, còn <b>{dsChuaTra.Count} ghế chưa trả</b>.<br>
Hạn giữ chỗ: <b>{E(Gio(don.HanGiuCho!.Value))}</b> (còn khoảng {E(ConLai(don))}).</p>
<table style=""border-collapse:collapse;font-size:14px"">
<tr style=""background:#f5f7fa""><th style=""padding:6px 10px;text-align:left"">Ghế</th><th style=""padding:6px 10px;text-align:left"">Người</th><th style=""padding:6px 10px;text-align:left"">Còn thiếu</th><th style=""padding:6px 10px;text-align:right"">Số tiền</th></tr>
{dong}
</table>
<p>{E(CauHetHan(don))} Thành viên đã nhận ghế và có email cũng được TravelVN nhắc riêng.</p>
<p><a href=""{E(linkDon)}"" style=""display:inline-block;padding:10px 18px;background:#0f766e;color:#fff;text-decoration:none;border-radius:6px"">Mở trang quản lý đơn</a></p>
{(linkMoi != null ? $@"<p>Link mời gửi lại cho nhóm: <a href=""{E(linkMoi)}"">{E(linkMoi)}</a></p>" : "")}
<p style=""color:#7b8794;font-size:13px"">Thư này gửi tự động từ TravelVN, bạn không cần trả lời.</p>
</div>";

            return new ThuEmail(tk.Email ?? "", tk.HoTen, tieuDe, html, chu.ToString());
        }
    }
}
