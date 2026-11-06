using System.Globalization;
using System.Text;
using System.Text.Json;

namespace webdulich.Services
{
    // Lấy đường xe chạy giữa hai điểm từ máy chủ OSRM công khai. Chỉ gọi lúc admin lưu tọa độ, kết quả lưu vào CSDL.
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
