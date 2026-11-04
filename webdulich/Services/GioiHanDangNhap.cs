using Microsoft.Extensions.Caching.Memory;

namespace webdulich.Services
{
    // Chặn dò mật khẩu: sai 5 lần liên tiếp với cùng tài khoản và cùng IP thì khóa 15 phút.
    // Lưu trong bộ nhớ của ứng dụng, khởi động lại thì mất bộ đếm (đủ cho việc này, không cần thêm bảng).
    // Đếm cả tài khoản không tồn tại, để không lộ tài khoản nào có thật.
    public class GioiHanDangNhap
    {
        public const int SoLanToiDa = 5;
        private static readonly TimeSpan ThoiGianKhoa = TimeSpan.FromMinutes(15);

        // SizeLimit: kẻ dò gửi hàng loạt tài khoản khác nhau cũng không làm đầy bộ nhớ
        private readonly MemoryCache _boDem = new MemoryCache(new MemoryCacheOptions { SizeLimit = 20000 });
        private readonly object _khoa = new object();

        private class DemSai
        {
            public int SoLan;
            public DateTime? KhoaDen;
        }

        private static string TaoKhoa(string taiKhoan, string? ip) => $"{taiKhoan.Trim().ToLowerInvariant()}|{ip}";

        private static TimeSpan? ThoiGianConLai(DemSai? dem)
        {
            var conLai = dem?.KhoaDen - DateTime.UtcNow;
            return conLai > TimeSpan.Zero ? conLai : null;
        }

        // Thời gian còn bị khóa; null là chưa khóa
        public TimeSpan? ConKhoa(string taiKhoan, string? ip)
        {
            lock (_khoa)
            {
                return ThoiGianConLai(_boDem.Get<DemSai>(TaoKhoa(taiKhoan, ip)));
            }
        }

        // Ghi một lần sai; trả về thời gian bị khóa nếu lần này đủ số lần tối đa
        public TimeSpan? GhiSai(string taiKhoan, string? ip)
        {
            lock (_khoa)
            {
                var khoa = TaoKhoa(taiKhoan, ip);
                var dem = _boDem.Get<DemSai>(khoa) ?? new DemSai();
                dem.SoLan++;
                if (dem.SoLan >= SoLanToiDa)
                {
                    dem.KhoaDen = DateTime.UtcNow.Add(ThoiGianKhoa);
                }
                _boDem.Set(khoa, dem, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = ThoiGianKhoa });
                return ThoiGianConLai(dem);
            }
        }

        // Đăng nhập đúng thì đếm lại từ đầu
        public void XoaDem(string taiKhoan, string? ip)
        {
            lock (_khoa)
            {
                _boDem.Remove(TaoKhoa(taiKhoan, ip));
            }
        }
    }
}
