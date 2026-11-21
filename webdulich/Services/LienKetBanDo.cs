using System.Globalization;

namespace webdulich.Services
{
    // Đường link mở Google Maps từ tọa độ có sẵn trong CSDL. Chỉ là link (Maps URLs), không gọi API, không cần khóa.
    // https://developers.google.com/maps/documentation/urls/get-started
    public static class LienKetBanDo
    {
        // Google cho tối đa 9 điểm dừng giữa đường (trình duyệt điện thoại có thể ít hơn); tour dài hơn thì bỏ bớt điểm ở giữa
        public const int SoDiemDungToiDa = 9;

        private static string Diem(decimal vi, decimal kinh) =>
            string.Create(CultureInfo.InvariantCulture, $"{vi:0.######},{kinh:0.######}");

        // Chỉ đường xe hơi từ điểm đầu tới điểm cuối, đi qua các điểm dừng (nếu có)
        public static string ChiDuong((decimal Vi, decimal Kinh) dau, (decimal Vi, decimal Kinh) cuoi,
            IReadOnlyList<(decimal Vi, decimal Kinh)>? diemDung = null)
        {
            var url = "https://www.google.com/maps/dir/?api=1&travelmode=driving"
                + "&origin=" + Diem(dau.Vi, dau.Kinh)
                + "&destination=" + Diem(cuoi.Vi, cuoi.Kinh);
            if (diemDung != null && diemDung.Count > 0)
            {
                url += "&waypoints=" + Uri.EscapeDataString(string.Join("|", ChonDiemDung(diemDung).Select(d => Diem(d.Vi, d.Kinh))));
            }
            return url;
        }

        // Một điểm trên Google Maps (ghim ở đúng tọa độ)
        public static string MoDiem(decimal vi, decimal kinh) =>
            "https://www.google.com/maps/search/?api=1&query=" + Diem(vi, kinh);

        // Street View gần tọa độ nhất (Google tự chọn ảnh gần đó, nơi không có ảnh thì báo không có)
        public static string StreetView(decimal vi, decimal kinh) =>
            "https://www.google.com/maps/@?api=1&map_action=pano&viewpoint=" + Diem(vi, kinh);

        // Bản đồ Google nhúng vào trang (iframe) theo kiểu "Chia sẻ, nhúng bản đồ", không cần khóa
        public static string Nhung(decimal vi, decimal kinh, int zoom = 14) =>
            string.Create(CultureInfo.InvariantCulture, $"https://maps.google.com/maps?q={Diem(vi, kinh)}&z={zoom}&hl=vi&output=embed");

        // Street View nhúng vào trang (tab Đi dạo 360°). Có khóa Maps Embed API thì dùng cách chính thức;
        // chưa có khóa thì dùng kiểu "Chia sẻ, nhúng" của Google Maps (không cần khóa, nhưng Google không công bố tài liệu nên có thể đổi).
        public static string NhungStreetView(decimal vi, decimal kinh, string? khoa)
        {
            if (!string.IsNullOrWhiteSpace(khoa))
            {
                return "https://www.google.com/maps/embed/v1/streetview?key=" + Uri.EscapeDataString(khoa.Trim())
                    + "&location=" + Diem(vi, kinh) + "&fov=90&language=vi";
            }
            return "https://maps.google.com/maps?layer=c&cbll=" + Diem(vi, kinh) + "&cbp=11,0,0,0,0&hl=vi&output=svembed";
        }

        // Đọc mã admin dán vào ô "Ảnh 360": cả đoạn HTML <iframe src="..."> từ nút "Nhúng bản đồ" của Google Maps, hoặc chỉ địa chỉ trong đó.
        // Chỉ nhận https://www.google.com/maps/embed?... (trùng CK_DiemThamQuan_LinkNhung360), tối đa 1000 ký tự. Ô trống: link = null, hợp lệ.
        public static bool DocMaNhung360(string? chu, out string? link)
        {
            link = null;
            if (string.IsNullOrWhiteSpace(chu))
            {
                return true;
            }

            var m = System.Text.RegularExpressions.Regex.Match(chu, "src\\s*=\\s*[\"']([^\"']+)[\"']", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var diaChi = System.Net.WebUtility.HtmlDecode(m.Success ? m.Groups[1].Value : chu).Trim();
            if (diaChi.Length > 1000
                || !Uri.TryCreate(diaChi, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || !string.Equals(uri.Host, "www.google.com", StringComparison.OrdinalIgnoreCase)
                || !uri.AbsolutePath.StartsWith("/maps/embed", StringComparison.Ordinal)
                || !diaChi.StartsWith("https://www.google.com/maps/embed?", StringComparison.Ordinal))
            {
                return false;
            }
            link = diaChi;
            return true;
        }

        // Giữ tối đa SoDiemDungToiDa điểm, rải đều trên hành trình
        private static IEnumerable<(decimal Vi, decimal Kinh)> ChonDiemDung(IReadOnlyList<(decimal Vi, decimal Kinh)> ds)
        {
            if (ds.Count <= SoDiemDungToiDa)
            {
                return ds;
            }
            return Enumerable.Range(0, SoDiemDungToiDa)
                .Select(i => ds[(int)Math.Round(i * (ds.Count - 1) / (double)(SoDiemDungToiDa - 1))]);
        }
    }
}
