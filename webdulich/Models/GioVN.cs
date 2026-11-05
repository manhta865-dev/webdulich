namespace webdulich.Models
{
    // Giờ Việt Nam (UTC+7, không đổi giờ mùa) cho mọi nghiệp vụ và hiển thị. Không dùng DateTime.Now, DateTime.Today:
    // máy chủ host chạy múi giờ khác nên giờ, hạn giữ chỗ và mốc "hôm nay" sẽ bị lệch.
    public static class GioVN
    {
        public static DateTime BayGio => DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);

        public static DateTime HomNay => BayGio.Date;

        // Giờ gửi xuống trình duyệt kèm múi giờ, để đồng hồ đếm ngược đúng dù máy người xem đặt múi giờ nào
        public static string Iso(DateTime gio) => gio.ToString("yyyy-MM-dd'T'HH:mm:ss") + "+07:00";
    }
}
