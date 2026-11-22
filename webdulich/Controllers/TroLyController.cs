using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webdulich.Models;

namespace webdulich.Controllers
{
    public class LuotTroLy
    {
        public bool LaKhach { get; set; }
        public string? Chu { get; set; }
    }

    public class YeuCauTroLy
    {
        public string? CauHoi { get; set; }
        public List<LuotTroLy>? LichSu { get; set; }
    }

    // Khung "Trợ lý TravelVN" (góc phải mọi trang) hỏi Gemini. Máy chủ gửi kèm thông tin thật: quy định đang chạy, liên hệ,
    // khuyến mãi đang hiện ở trang chủ, tour đang bán. Không gửi thông tin tài khoản hay đơn của khách.
    // Chưa có khóa hoặc AI lỗi thì trả duPhong = true để trình duyệt trả lời theo từ khóa như cũ.
    public class TroLyController : Controller
    {
        private readonly DuLichDbContext _context;

        public TroLyController(DuLichDbContext context)
        {
            _context = context;
        }

        private const int SoCauMoiGio = 20;
        private const int DoDaiToiDa = 500;
        private const int SoLuotNho = 6;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hoi([FromBody] YeuCauTroLy? yeuCau, [FromServices] IConfiguration cauHinh)
        {
            var cauHoi = yeuCau?.CauHoi?.Trim() ?? string.Empty;
            if (cauHoi.Length == 0)
            {
                return BadRequest(new { loi = "Bạn chưa nhập câu hỏi." });
            }
            if (cauHoi.Length > DoDaiToiDa)
            {
                cauHoi = cauHoi[..DoDaiToiDa];
            }

            var ai = new webdulich.Services.GoiYBangGemini(cauHinh);
            if (!ai.CoKhoa)
            {
                return Json(new { ok = false, duPhong = true });
            }

            // Đếm câu hỏi trong phiên: quá SoCauMoiGio câu trong 60 phút thì trả lời theo từ khóa
            var bayGio = DateTime.UtcNow;
            var tu = long.TryParse(HttpContext.Session.GetString("tlTu"), out var ticks) ? new DateTime(ticks, DateTimeKind.Utc) : bayGio;
            var soLan = bayGio - tu > TimeSpan.FromHours(1) ? 0 : HttpContext.Session.GetInt32("tlLan") ?? 0;
            if (soLan == 0)
            {
                tu = bayGio;
            }
            if (soLan >= SoCauMoiGio)
            {
                return Json(new { ok = false, duPhong = true, loi = "Bạn đã hỏi nhiều trong một giờ, trợ lý tạm trả lời theo mẫu." });
            }
            HttpContext.Session.SetString("tlTu", tu.Ticks.ToString());
            HttpContext.Session.SetInt32("tlLan", soLan + 1);

            var dsTour = TourChoAiDoc.LayDanhSach(_context);
            var lichSu = (yeuCau?.LichSu ?? new List<LuotTroLy>())
                .Where(l => !string.IsNullOrWhiteSpace(l.Chu))
                .TakeLast(SoLuotNho)
                .Select(l => (l.LaKhach, l.Chu!.Trim()))
                .ToList();
            // Gemini cần lượt đầu là của khách: bỏ các câu chào sẵn của trợ lý ở đầu
            while (lichSu.Count > 0 && !lichSu[0].LaKhach)
            {
                lichSu.RemoveAt(0);
            }

            var (traLoi, loi) = await ai.HoiTroLyAsync(SoanThongTin(cauHinh, dsTour), lichSu, cauHoi, dsTour.Select(t => t.Id).ToHashSet());
            if (traLoi == null)
            {
                return Json(new { ok = false, duPhong = true, loi });
            }

            return Json(new
            {
                ok = true,
                traLoi = traLoi.TraLoi,
                tour = traLoi.DsTourId
                    .Select(id => dsTour.First(t => t.Id == id))
                    .Select(t => new { ten = t.Ten, url = Url.Action("ChiTiet", "DiaDiem", new { id = t.Id }) })
            });
        }

        // Thông tin thật của TravelVN cho trợ lý: số giờ lấy từ cấu hình, khuyến mãi lấy đúng điều kiện trang chủ đang hiện
        private string SoanThongTin(IConfiguration cauHinh, List<webdulich.Services.TourChoAi> dsTour)
        {
            int soGioGiuCho = cauHinh.GetValue("DonNhom:SoGioGiuCho", 24);
            int soGioGiaHan = cauHinh.GetValue("DonNhom:SoGioGiaHan", 6);
            var homNay = GioVN.HomNay;
            var dsKhuyenMai = _context.KhuyenMais.AsNoTracking()
                .Where(x => x.TrangThai == TrangThaiKhuyenMai.HoatDong && x.NgayBatDau <= homNay && x.NgayKetThuc >= homNay
                    && (!x.SoLuotDungToiDa.HasValue || x.DatTours.Count(d => d.TrangThai != "DaHuy") < x.SoLuotDungToiDa.Value))
                .OrderBy(x => x.ThuTu)
                .Take(8)
                .Select(x => new { x.MaKhuyenMai, x.TenKhuyenMai, x.KieuGiamGia, x.GiaTriGiam, x.NgayKetThuc })
                .ToList();

            var chu = new StringBuilder();
            chu.AppendLine("Liên hệ: hotline và Zalo 0342500619; trên web có nút chat với admin và nút Zalo ở góc phải.");
            chu.AppendLine("Cách đặt riêng: mở trang tour, bấm Chọn ngày khởi hành, chọn lịch còn chỗ, nhập thông tin hành khách, chọn chỗ ngồi nếu xe có sơ đồ, rồi thanh toán.");
            chu.AppendLine($"Giữ chỗ: đặt xong chỗ được giữ {soGioGiuCho} giờ để thanh toán, muộn nhất đến cuối ngày trước ngày khởi hành.");
            chu.AppendLine("Thanh toán: chuyển khoản ngân hàng bằng mã QR (số tiền và nội dung là mã chuyến đi điền sẵn), ví MoMo, hoặc tiền mặt. Chuyển đúng tiền, đúng nội dung thì hệ thống tự xác nhận khi nhận được tiền. Chọn tiền mặt mà quá hạn giữ chỗ chưa trả thì đơn tự hủy; đã báo chuyển khoản thì không tự hủy trong lúc chờ đối chiếu.");
            chu.AppendLine($"Đặt cho nhóm (từ 2 ghế): một người giữ chỗ cho cả nhóm và gửi link mời; mỗi người tự nhận một ghế, tự điền thông tin và tự trả phần của mình bằng mã QR riêng của ghế. Cả nhóm có {soGioGiuCho} giờ; hết hạn lần đầu mà còn ghế chưa trả thì được gia hạn thêm {soGioGiaHan} giờ một lần, sau đó ghế chưa trả bị nhả. Người đặt thấy ai đã trả nhưng không thấy giấy tờ người khác, có thể trả hộ, bớt ghế, chốt sớm.");
            chu.AppendLine("Hủy đơn: tự hủy được đơn đặt riêng khi đơn còn giữ chỗ, chưa thanh toán, không có chuyển khoản đang chờ và chưa tới ngày đi (ở mục Lịch sử đặt tour). Đơn nhóm chỉ tự hủy được khi chưa ai trả. Đã thanh toán hoặc cần hoàn tiền thì liên hệ TravelVN.");
            chu.AppendLine("Mục Lịch sử đặt tour: xem đơn, xem vé khi đã thanh toán, đổi chỗ ngồi, hủy đơn đủ điều kiện, viết đánh giá sau chuyến đi.");
            chu.AppendLine("Trang hữu ích: Gợi ý tour (chọn tiêu chí hoặc hỏi AI), Bản đồ tour, Lịch khởi hành theo tháng, Bài viết. Trang tour có bản đồ hành trình, lịch trình theo buổi; tour có tọa độ có thêm Khám phá 3D (bản đồ 3D, chỉ đường, xem 360 độ).");

            if (dsKhuyenMai.Any())
            {
                chu.AppendLine("Khuyến mãi đang áp dụng (nhập mã ở bước thanh toán, hoặc lúc tạo đơn nhóm):");
                foreach (var k in dsKhuyenMai)
                {
                    var giam = k.KieuGiamGia == "PhanTram"
                        ? string.Create(CultureInfo.InvariantCulture, $"giảm {k.GiaTriGiam:0.##}%")
                        : string.Create(CultureInfo.InvariantCulture, $"giảm {k.GiaTriGiam:0} VNĐ");
                    chu.AppendLine($"- mã {k.MaKhuyenMai}: {k.TenKhuyenMai}, {giam}, đến hết {k.NgayKetThuc:dd/MM/yyyy}");
                }
            }
            else
            {
                chu.AppendLine("Hiện không có mã khuyến mãi nào đang áp dụng.");
            }

            chu.AppendLine($"Hôm nay là {homNay:dd/MM/yyyy}.");
            chu.AppendLine("Danh sách tour đang bán:");
            chu.Append(webdulich.Services.GoiYBangGemini.DanhSachTour(dsTour));
            return chu.ToString();
        }
    }
}
