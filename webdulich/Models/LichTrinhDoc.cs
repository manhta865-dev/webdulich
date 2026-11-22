using System.Text.RegularExpressions;

namespace webdulich.Models
{
    // Một hoạt động có giờ trong nội dung một ngày lịch trình
    public class HoatDongTheoGio
    {
        public string Gio { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
    }

    // Các hoạt động của một buổi (Sáng, Trưa, Chiều, Tối)
    public class BuoiTrongNgay
    {
        public string Ten { get; set; } = string.Empty;
        public List<HoatDongTheoGio> DsHoatDong { get; set; } = new();
    }

    // Đọc nội dung chữ của một ngày lịch trình (admin nhập tự do) để trang tour chia theo buổi.
    // Dòng có giờ ở đầu như "* 06:00 – 07:00: Khởi hành" hoặc "06:00 : Khởi hành" được xếp vào buổi theo giờ bắt đầu;
    // dòng tiêu đề bắt đầu bằng # bị bỏ (trùng tên ngày); dòng còn lại giữ làm ghi chú. Không có dòng nào có giờ thì trang hiện nguyên văn.
    // Chỉ đọc để hiển thị, không ghi gì vào CSDL.
    public static class LichTrinhDoc
    {
        private static readonly Regex DongCoGio = new(
            @"^\s*[\*\-•+]?\s*(\d{1,2})\s*[:hH]\s*(\d{2})(?:\s*[–—-]\s*(\d{1,2})\s*[:hH]\s*(\d{2}))?\s*[:：–—-]?\s*(.+)$",
            RegexOptions.Compiled);

        private static readonly string[] ThuTuBuoi = { "Sáng", "Trưa", "Chiều", "Tối" };

        private static string BuoiCuaGio(int gio) => gio < 11 ? "Sáng" : gio < 14 ? "Trưa" : gio < 18 ? "Chiều" : "Tối";

        public static (List<BuoiTrongNgay> DsBuoi, List<string> GhiChu) TachTheoBuoi(string? noiDung)
        {
            var theoBuoi = new Dictionary<string, BuoiTrongNgay>();
            var ghiChu = new List<string>();
            if (string.IsNullOrWhiteSpace(noiDung))
            {
                return (new List<BuoiTrongNgay>(), ghiChu);
            }

            foreach (var dongGoc in noiDung.Replace("\r", string.Empty).Split('\n'))
            {
                var dong = dongGoc.Trim();
                if (dong.Length == 0 || dong.StartsWith('#'))
                {
                    continue;
                }

                var m = DongCoGio.Match(dong);
                if (m.Success && int.TryParse(m.Groups[1].Value, out var gio) && gio <= 23 && int.TryParse(m.Groups[2].Value, out var phut) && phut <= 59)
                {
                    var gioChu = $"{gio:00}:{phut:00}";
                    if (m.Groups[3].Success && int.TryParse(m.Groups[3].Value, out var gioDen) && int.TryParse(m.Groups[4].Value, out var phutDen))
                    {
                        gioChu += $" – {gioDen:00}:{phutDen:00}";
                    }
                    var ten = BuoiCuaGio(gio);
                    if (!theoBuoi.TryGetValue(ten, out var buoi))
                    {
                        theoBuoi[ten] = buoi = new BuoiTrongNgay { Ten = ten };
                    }
                    buoi.DsHoatDong.Add(new HoatDongTheoGio { Gio = gioChu, NoiDung = m.Groups[5].Value.Trim() });
                }
                else
                {
                    ghiChu.Add(dong.TrimStart('*', '-', '•', '+', ' '));
                }
            }

            return (ThuTuBuoi.Where(theoBuoi.ContainsKey).Select(t => theoBuoi[t]).ToList(), ghiChu);
        }
    }
}
