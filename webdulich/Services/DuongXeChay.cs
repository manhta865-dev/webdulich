using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace webdulich.Services
{
    // Chỉ đường từng bước của một đoạn (một ngày trong lịch trình), đã dịch sang tiếng Việt
    public class ChiDuongDoan
    {
        public double Km { get; set; }
        public double Phut { get; set; }
        public List<BuocChiDuong> DsBuoc { get; set; } = new();
    }

    public class BuocChiDuong
    {
        public string Chu { get; set; } = string.Empty;
        // Quãng đi tiếp sau thao tác này, tới thao tác kế tiếp
        public double Km { get; set; }
    }

    // Lấy đường xe chạy giữa hai điểm từ máy chủ OSRM công khai. Đường vẽ (LayAsync) chỉ gọi lúc admin lưu tọa độ, kết quả lưu vào CSDL;
    // chỉ đường từng bước (LayChiDanAsync) gọi khi người xem cần, nhớ tạm trong bộ nhớ.
    // Máy chủ này không cam kết ổn định nên mọi lỗi đều trả về null, nơi gọi tự báo admin và bản đồ dùng nét đứt thẳng.
    public static class DuongXeChay
    {
        private const string MayChu = "https://router.project-osrm.org/route/v1/driving/";

        // Sai lệch cho phép khi rút gọn, khoảng 30 mét: bớt điểm thừa để trang tour không phải tải dữ liệu nặng
        private const double SaiLechRutGon = 0.0003;

        private static readonly HttpClient _http = TaoHttp();

        private static HttpClient TaoHttp()
        {
            var http = new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(10) })
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
            // Chính sách của OSRM yêu cầu định danh ứng dụng
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TravelVN-DoAn/1.0");
            return http;
        }

        public static async Task<string?> LayAsync(decimal kinhDau, decimal viDau, decimal kinhCuoi, decimal viCuoi)
        {
            var url = string.Create(CultureInfo.InvariantCulture,
                $"{MayChu}{kinhDau},{viDau};{kinhCuoi},{viCuoi}?overview=full&geometries=geojson");
            try
            {
                using var phanHoi = await _http.GetAsync(url);
                if (!phanHoi.IsSuccessStatusCode)
                {
                    return null;
                }

                using var tep = JsonDocument.Parse(await phanHoi.Content.ReadAsStringAsync());
                var goc = tep.RootElement;
                if (goc.GetProperty("code").GetString() != "Ok" || goc.GetProperty("routes").GetArrayLength() == 0)
                {
                    return null;
                }

                var dsDiem = goc.GetProperty("routes")[0].GetProperty("geometry").GetProperty("coordinates")
                    .EnumerateArray()
                    .Select(d => (Kinh: d[0].GetDouble(), Vi: d[1].GetDouble()))
                    .ToList();
                return dsDiem.Count < 2 ? null : ThanhJson(RutGon(dsDiem));
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
            {
                return null;
            }
        }

        // ---------- Chỉ đường từng bước (trang Khám phá 3D) ----------
        // Gọi lúc người xem bấm "Dẫn đường", không lưu CSDL. Nhớ tạm trong bộ nhớ 1 ngày theo cặp tọa độ để không hỏi OSRM nhiều lần;
        // lỗi thì nhớ 10 phút rồi mới thử lại. Khởi động lại web là mất, không sao.
        private static readonly ConcurrentDictionary<string, (DateTime HetHan, ChiDuongDoan? KetQua)> _nhoChiDuong = new();
        private const int SoMucNhoToiDa = 500;

        public static async Task<ChiDuongDoan?> LayChiDanAsync(decimal kinhDau, decimal viDau, decimal kinhCuoi, decimal viCuoi)
        {
            var khoa = string.Create(CultureInfo.InvariantCulture, $"{kinhDau},{viDau};{kinhCuoi},{viCuoi}");
            if (_nhoChiDuong.TryGetValue(khoa, out var daNho) && daNho.HetHan > DateTime.UtcNow)
            {
                return daNho.KetQua;
            }

            var ketQua = await GoiChiDanAsync(khoa);
            if (_nhoChiDuong.Count >= SoMucNhoToiDa)
            {
                _nhoChiDuong.Clear();
            }
            _nhoChiDuong[khoa] = (DateTime.UtcNow.Add(ketQua == null ? TimeSpan.FromMinutes(10) : TimeSpan.FromDays(1)), ketQua);
            return ketQua;
        }

        private static async Task<ChiDuongDoan?> GoiChiDanAsync(string toaDo)
        {
            try
            {
                using var phanHoi = await _http.GetAsync($"{MayChu}{toaDo}?overview=false&steps=true");
                if (!phanHoi.IsSuccessStatusCode)
                {
                    return null;
                }

                using var tep = JsonDocument.Parse(await phanHoi.Content.ReadAsStringAsync());
                var goc = tep.RootElement;
                if (goc.GetProperty("code").GetString() != "Ok" || goc.GetProperty("routes").GetArrayLength() == 0)
                {
                    return null;
                }

                var tuyen = goc.GetProperty("routes")[0];
                var ketQua = new ChiDuongDoan
                {
                    Km = tuyen.GetProperty("distance").GetDouble() / 1000,
                    Phut = tuyen.GetProperty("duration").GetDouble() / 60
                };

                foreach (var chang in tuyen.GetProperty("legs").EnumerateArray())
                {
                    foreach (var buoc in chang.GetProperty("steps").EnumerateArray())
                    {
                        var chu = DichBuoc(buoc);
                        if (chu == null)
                        {
                            continue;
                        }
                        double km = buoc.GetProperty("distance").GetDouble() / 1000;
                        // Hai bước liền nhau cùng một câu (đường đổi tên nhưng vẫn đi thẳng): gộp lại cho gọn
                        var truoc = ketQua.DsBuoc.LastOrDefault();
                        if (truoc != null && truoc.Chu == chu)
                        {
                            truoc.Km += km;
                            continue;
                        }
                        ketQua.DsBuoc.Add(new BuocChiDuong { Chu = chu, Km = km });
                    }
                }
                return ketQua.DsBuoc.Count == 0 ? null : ketQua;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
            {
                return null;
            }
        }

        // Một bước của OSRM (maneuver.type, modifier, exit, tên đường) thành một câu tiếng Việt. Trả null nếu bỏ qua bước đó.
        private static string? DichBuoc(JsonElement buoc)
        {
            var thaoTac = buoc.GetProperty("maneuver");
            string loai = thaoTac.GetProperty("type").GetString() ?? string.Empty;
            string? huong = thaoTac.TryGetProperty("modifier", out var m) ? m.GetString() : null;
            int? loiRa = thaoTac.TryGetProperty("exit", out var e) && e.ValueKind == JsonValueKind.Number ? e.GetInt32() : null;

            string ten = buoc.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            string soHieu = buoc.TryGetProperty("ref", out var r) ? r.GetString() ?? string.Empty : string.Empty;
            string duong = (ten, soHieu) switch
            {
                ("", "") => string.Empty,
                ("", _) => soHieu,
                (_, "") => ten,
                _ when ten.Contains(soHieu, StringComparison.OrdinalIgnoreCase) => ten,
                _ => $"{ten} ({soHieu})"
            };
            string vao = duong.Length > 0 ? " vào " + duong : string.Empty;
            string theo = duong.Length > 0 ? " theo " + duong : string.Empty;

            string HuongRe() => huong switch
            {
                "right" => "Rẽ phải",
                "left" => "Rẽ trái",
                "slight right" => "Chếch sang phải",
                "slight left" => "Chếch sang trái",
                "sharp right" => "Rẽ gắt sang phải",
                "sharp left" => "Rẽ gắt sang trái",
                "uturn" => "Quay đầu",
                _ => "Đi thẳng"
            };
            string BenNao() => huong != null && huong.Contains("left") ? "bên trái" : huong != null && huong.Contains("right") ? "bên phải" : "giữa";

            return loai switch
            {
                "depart" => "Xuất phát" + theo,
                "arrive" => "Đến nơi" + (huong == "left" ? ", điểm đến ở bên trái" : huong == "right" ? ", điểm đến ở bên phải" : string.Empty),
                "turn" or "end of road" => (loai == "end of road" ? "Hết đường, " + HuongRe().ToLowerInvariant() : HuongRe()) + vao,
                "new name" or "continue" => huong is null or "straight" ? "Đi tiếp" + theo : HuongRe() + vao,
                "merge" => "Nhập vào " + (duong.Length > 0 ? duong : "làn đường chính"),
                "on ramp" => "Lên đường dẫn " + BenNao() + vao,
                "off ramp" => "Ra lối " + BenNao() + vao,
                "fork" => "Ở ngã rẽ, đi về phía " + BenNao() + vao,
                "roundabout" or "rotary" => "Vào vòng xuyến" + (loiRa.HasValue ? $", ra ở lối thứ {loiRa}" : string.Empty) + vao,
                "roundabout turn" => HuongRe() + " ở vòng xuyến" + vao,
                "exit roundabout" or "exit rotary" => "Ra khỏi vòng xuyến" + vao,
                "notification" => null,
                _ => HuongRe() + vao
            };
        }

        // Giải thuật Ramer-Douglas-Peucker: giữ hai đầu, giữ điểm lệch xa nhất rồi xử lý tiếp hai đoạn con
        private static List<(double Kinh, double Vi)> RutGon(List<(double Kinh, double Vi)> ds)
        {
            var giuLai = new bool[ds.Count];
            giuLai[0] = giuLai[^1] = true;
            var ngan = new Stack<(int Dau, int Cuoi)>();
            ngan.Push((0, ds.Count - 1));

            while (ngan.Count > 0)
            {
                var (dau, cuoi) = ngan.Pop();
                double lechNhat = 0;
                int viTri = -1;
                for (int i = dau + 1; i < cuoi; i++)
                {
                    double lech = KhoangCachToiDoan(ds[i], ds[dau], ds[cuoi]);
                    if (lech > lechNhat)
                    {
                        lechNhat = lech;
                        viTri = i;
                    }
                }
                if (viTri >= 0 && lechNhat > SaiLechRutGon)
                {
                    giuLai[viTri] = true;
                    ngan.Push((dau, viTri));
                    ngan.Push((viTri, cuoi));
                }
            }

            return ds.Where((_, i) => giuLai[i]).ToList();
        }

        private static double KhoangCachToiDoan((double Kinh, double Vi) p, (double Kinh, double Vi) a, (double Kinh, double Vi) b)
        {
            double dx = b.Kinh - a.Kinh, dy = b.Vi - a.Vi;
            double doDai = dx * dx + dy * dy;
            double t = doDai == 0 ? 0 : Math.Clamp(((p.Kinh - a.Kinh) * dx + (p.Vi - a.Vi) * dy) / doDai, 0, 1);
            double x = p.Kinh - (a.Kinh + t * dx), y = p.Vi - (a.Vi + t * dy);
            return Math.Sqrt(x * x + y * y);
        }

        private static string ThanhJson(List<(double Kinh, double Vi)> ds)
        {
            var chu = new StringBuilder("[");
            for (int i = 0; i < ds.Count; i++)
            {
                if (i > 0)
                {
                    chu.Append(',');
                }
                chu.Append(string.Create(CultureInfo.InvariantCulture, $"[{Math.Round(ds[i].Kinh, 5)},{Math.Round(ds[i].Vi, 5)}]"));
            }
            return chu.Append(']').ToString();
        }
    }
}
