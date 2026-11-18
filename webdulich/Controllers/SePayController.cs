using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using webdulich.Models;
using webdulich.Services;

namespace webdulich.Controllers
{
    // Cửa nhận tin "có tiền vào" từ SePay (RB-48): POST /api/sepay/tien-vao, header "Authorization: Apikey <khóa>".
    // Khóa đặt ở cấu hình ThanhToanTuDong:SePayApiKey (cấu hình riêng của host, không commit). Chưa có khóa thì cửa này tắt (404).
    // Luôn trả 200 {"success": true} khi đã nhận được (kể cả không khớp đơn nào, hoặc báo trùng) để SePay không gửi lại mãi;
    // sai khóa trả 401, gói tin hỏng trả 400.
    [ApiController]
    [Route("api/sepay")]
    [IgnoreAntiforgeryToken]
    public class SePayController : ControllerBase
    {
        private readonly DuLichDbContext _context;
        private readonly IConfiguration _cauHinh;
        private readonly ILogger<SePayController> _nhatKy;

        public SePayController(DuLichDbContext context, IConfiguration cauHinh, ILogger<SePayController> nhatKy)
        {
            _context = context;
            _cauHinh = cauHinh;
            _nhatKy = nhatKy;
        }

        [HttpPost("tien-vao")]
        public IActionResult TienVao([FromBody] JsonElement goiTin)
        {
            var khoa = _cauHinh["ThanhToanTuDong:SePayApiKey"];
            if (string.IsNullOrWhiteSpace(khoa))
            {
                return NotFound(new { success = false, message = "Chưa bật tự xác nhận chuyển khoản." });
            }
            if (!DungKhoa(Request.Headers.Authorization.ToString(), khoa))
            {
                _nhatKy.LogWarning("SePay: gói tin sai khóa từ {Ip}", HttpContext.Connection.RemoteIpAddress);
                return StatusCode(401, new { success = false, message = "Sai khóa." });
            }

            if (goiTin.ValueKind != JsonValueKind.Object)
            {
                return BadRequest(new { success = false, message = "Gói tin không đúng định dạng." });
            }

            // Chỉ xử lý tiền vào
            if (!string.Equals(Chuoi(goiTin, "transferType"), "in", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { success = true, message = "Bỏ qua giao dịch không phải tiền vào." });
            }

            var maGiaoDich = Chuoi(goiTin, "id");
            var soTien = So(goiTin, "transferAmount");
            if (string.IsNullOrWhiteSpace(maGiaoDich) || soTien is not > 0)
            {
                return BadRequest(new { success = false, message = "Thiếu mã giao dịch hoặc số tiền." });
            }

            // Nội dung để tìm mã: mã thanh toán SePay tách sẵn (nếu có) + nội dung chuyển khoản + mô tả đầy đủ của ngân hàng
            var noiDung = string.Join(" ", new[] { Chuoi(goiTin, "code"), Chuoi(goiTin, "content"), Chuoi(goiTin, "description") }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var tienVao = new TienVao(
                NguonTienVao.SePay,
                maGiaoDich,
                Chuoi(goiTin, "gateway"),
                Chuoi(goiTin, "accountNumber"),
                DocThoiGian(Chuoi(goiTin, "transactionDate")),
                soTien.Value,
                noiDung,
                Chuoi(goiTin, "referenceCode"));

            var ketQua = DoiChieuTienVao.Nhan(_context, tienVao);
            _nhatKy.LogInformation("SePay: tiền vào {Ma} {SoTien} -> {KetQua}{Trung}", maGiaoDich, soTien, ketQua.Dong.KetQua, ketQua.LaTrung ? " (báo trùng)" : "");
            return Ok(new { success = true });
        }

        // So khóa theo thời gian cố định để không đoán dần được khóa qua thời gian phản hồi
        private static bool DungKhoa(string header, string khoa)
        {
            const string tienTo = "Apikey ";
            if (!header.StartsWith(tienTo, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var guiLen = Encoding.UTF8.GetBytes(header[tienTo.Length..].Trim());
            var dung = Encoding.UTF8.GetBytes(khoa.Trim());
            return CryptographicOperations.FixedTimeEquals(guiLen, dung);
        }

        private static string? Chuoi(JsonElement o, string ten)
        {
            if (!o.TryGetProperty(ten, out var v))
            {
                return null;
            }
            return v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                _ => null
            };
        }

        private static decimal? So(JsonElement o, string ten)
        {
            if (!o.TryGetProperty(ten, out var v))
            {
                return null;
            }
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d))
            {
                return d;
            }
            if (v.ValueKind == JsonValueKind.String && decimal.TryParse(v.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var s))
            {
                return s;
            }
            return null;
        }

        // SePay gửi "yyyy-MM-dd HH:mm:ss" theo giờ Việt Nam; đọc không được thì lấy giờ nhận
        private static DateTime DocThoiGian(string? s) =>
            DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
                ? t : GioVN.BayGio;
    }
}
