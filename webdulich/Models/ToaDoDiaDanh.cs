using System.Globalization;
using System.Text;

namespace webdulich.Models
{
    // Tọa độ tham khảo của các tỉnh, thành, điểm du lịch Việt Nam. Từ Mo18 vị trí thật nằm ở DiaDiems.ViDo, KinhDo (RB-44);
    // bảng này chỉ để GỢI Ý: admin gõ tên địa điểm ở form tour thì bản đồ chọn vị trí tự nhảy tới đây, admin sửa lại được.
    // Đây là dữ liệu tra cứu cố định, không phải dữ liệu nghiệp vụ.
    public static class ToaDoDiaDanh
    {
        private static readonly Dictionary<string, (double Vi, double Kinh)> Bang = new()
        {
            ["ha noi"] = (21.028, 105.854), ["ha long"] = (20.951, 107.080), ["quang ninh"] = (20.951, 107.080),
            ["cat ba"] = (20.727, 107.048), ["ninh binh"] = (20.251, 105.975), ["sapa"] = (22.336, 103.844),
            ["sa pa"] = (22.336, 103.844), ["lao cai"] = (22.486, 103.970), ["ha giang"] = (22.823, 104.984),
            ["moc chau"] = (20.849, 104.637), ["hai phong"] = (20.845, 106.688),
            ["quang binh"] = (17.468, 106.623), ["hue"] = (16.463, 107.590), ["da nang"] = (16.054, 108.202),
            ["hoi an"] = (15.880, 108.335), ["quy nhon"] = (13.782, 109.219), ["phu yen"] = (13.088, 109.092),
            ["nha trang"] = (12.238, 109.196), ["khanh hoa"] = (12.238, 109.196), ["da lat"] = (11.940, 108.458),
            ["lam dong"] = (11.940, 108.458), ["buon ma thuot"] = (12.667, 108.038), ["phan thiet"] = (10.928, 108.102),
            ["mui ne"] = (10.933, 108.287),
            ["ho chi minh"] = (10.776, 106.700), ["sai gon"] = (10.776, 106.700), ["vung tau"] = (10.346, 107.084),
            ["tay ninh"] = (11.310, 106.098), ["ben tre"] = (10.241, 106.376), ["can tho"] = (10.045, 105.747),
            ["an giang"] = (10.521, 105.125), ["chau doc"] = (10.700, 105.117), ["phu quoc"] = (10.289, 103.984),
            ["con dao"] = (8.683, 106.609), ["ca mau"] = (9.177, 105.150),
            // Bổ sung các điểm du lịch hay gặp
            ["mai chau"] = (20.663, 105.083), ["trang an"] = (20.255, 105.894), ["tam coc"] = (20.215, 105.937),
            ["tam dao"] = (21.459, 105.646), ["pu luong"] = (20.469, 105.209), ["sam son"] = (19.743, 105.904),
            ["cua lo"] = (18.818, 105.721), ["phong nha"] = (17.590, 106.283), ["ly son"] = (15.381, 109.118),
            ["quang ngai"] = (15.121, 108.804), ["kon tum"] = (14.350, 108.000), ["mang den"] = (14.613, 108.288),
            ["pleiku"] = (13.983, 108.000), ["gia lai"] = (13.983, 108.000), ["dak lak"] = (12.667, 108.038),
            ["ninh thuan"] = (11.565, 108.988), ["phan rang"] = (11.565, 108.988), ["binh ba"] = (11.840, 109.230),
            ["ho tram"] = (10.483, 107.483), ["my tho"] = (10.360, 106.360), ["ha tien"] = (10.383, 104.488),
            ["rach gia"] = (10.012, 105.081), ["bac lieu"] = (9.285, 105.724), ["soc trang"] = (9.603, 105.980),
            ["dong thap"] = (10.460, 105.640), ["cao bang"] = (22.666, 106.258), ["ban gioc"] = (22.855, 106.723),
            ["dien bien"] = (21.386, 103.023), ["lai chau"] = (22.396, 103.458), ["ha nam"] = (20.541, 105.912),
            ["thanh hoa"] = (19.807, 105.776), ["vinh"] = (18.679, 105.681), ["nghe an"] = (18.679, 105.681),
            ["dong hoi"] = (17.468, 106.623), ["quang tri"] = (16.750, 107.185), ["bana"] = (15.995, 107.996),
            ["ba na"] = (15.995, 107.996), ["binh dinh"] = (13.782, 109.219), ["bao loc"] = (11.548, 107.807)
        };

        // Điểm đến nước ngoài hay gặp, chỉ dùng để gợi ý ở form tour (không dùng để chia miền)
        private static readonly Dictionary<string, (double Vi, double Kinh)> NuocNgoai = new()
        {
            ["usa"] = (34.052, -118.244), ["my"] = (34.052, -118.244), ["los angeles"] = (34.052, -118.244),
            ["new york"] = (40.713, -74.006), ["san francisco"] = (37.775, -122.419), ["las vegas"] = (36.170, -115.140),
            ["han quoc"] = (37.566, 126.978), ["seoul"] = (37.566, 126.978), ["nhat ban"] = (35.676, 139.650),
            ["tokyo"] = (35.676, 139.650), ["osaka"] = (34.694, 135.502), ["thai lan"] = (13.756, 100.502),
            ["bangkok"] = (13.756, 100.502), ["phuket"] = (7.880, 98.392), ["singapore"] = (1.352, 103.820),
            ["malaysia"] = (3.139, 101.687), ["kuala lumpur"] = (3.139, 101.687), ["bali"] = (-8.341, 115.092),
            ["campuchia"] = (11.556, 104.928), ["siem reap"] = (13.367, 103.844), ["lao"] = (17.975, 102.633),
            ["trung quoc"] = (39.904, 116.407), ["bac kinh"] = (39.904, 116.407), ["thuong hai"] = (31.230, 121.474),
            ["hong kong"] = (22.320, 114.169), ["dai loan"] = (25.033, 121.565), ["dubai"] = (25.205, 55.271),
            ["phap"] = (48.857, 2.352), ["paris"] = (48.857, 2.352), ["anh"] = (51.507, -0.128), ["london"] = (51.507, -0.128),
            ["y"] = (41.903, 12.496), ["rome"] = (41.903, 12.496), ["thuy si"] = (46.948, 7.447), ["uc"] = (-33.869, 151.209),
            ["sydney"] = (-33.869, 151.209), ["canada"] = (43.653, -79.383)
        };

        // Bảng gợi ý cho bản đồ chọn vị trí ở form tour (tên đã bỏ dấu → [vĩ độ, kinh độ]), gồm trong nước và nước ngoài
        public static IReadOnlyDictionary<string, double[]> BangGoiY() =>
            Bang.Concat(NuocNgoai).ToDictionary(x => x.Key, x => new[] { x.Value.Vi, x.Value.Kinh });

        // Bỏ dấu, viết thường để so tên: "Đà Lạt", "DA LAT", "Du lịch Đà Lạt" đều khớp "da lat"
        public static string BoDau(string chu)
        {
            var chuan = chu.Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in chuan)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().ToLowerInvariant().Trim();
        }

        // Tìm theo tên địa điểm trước, không có thì theo tên tour. Không khớp (ví dụ tour nước ngoài) thì null.
        public static (double Vi, double Kinh)? Tim(params string?[] dsTen)
        {
            foreach (var ten in dsTen.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                var k = BoDau(ten!);
                if (Bang.TryGetValue(k, out var td)) return td;
                var khop = Bang.Keys.Where(key => k.Contains(key)).OrderByDescending(key => key.Length).FirstOrDefault();
                if (khop != null) return Bang[khop];
            }
            return null;
        }

        // Tên điểm đến để hiển thị: dữ liệu cũ hay viết hoa toàn bộ ("HÀ GIANG") thì đưa về dạng tên riêng ("Hà Giang"),
        // nhưng giữ nguyên tên chỉ là một chữ viết tắt ngắn như "USA", "UK"
        public static string TenHienThi(string? ten)
        {
            var goc = (ten ?? "").Trim();
            if (goc.Length <= 3 && !goc.Contains(' ')) return goc;
            return CultureInfo.GetCultureInfo("vi-VN").TextInfo.ToTitleCase(goc.ToLower());
        }

        // Vùng theo vĩ độ (đủ dùng để nhóm điểm đến trên bản đồ)
        public static string Vung(double vi) => vi >= 19.3 ? "Miền Bắc" : vi >= 11.5 ? "Miền Trung và Tây Nguyên" : "Miền Nam";
    }
}
