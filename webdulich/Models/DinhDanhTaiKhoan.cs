using System.Text.RegularExpressions;

namespace webdulich.Models
{
    // Quy tắc chung cho email và số điện thoại của tài khoản (dùng ở đăng ký, đăng nhập, hồ sơ, admin).
    // Mỗi tài khoản có Email hoặc SoDienThoai hoặc cả hai; cột TaiKhoanDangNhap chỉ còn ghi kèm
    // cho đến khi bị bỏ ở bước thu hẹp (docs/MO_HINH_DU_LIEU.md).
    public static class DinhDanhTaiKhoan
    {
        private static readonly Regex MauEmail = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex MauSoDienThoai = new(@"^0\d{9}$");

        public static bool LaEmail(string? giaTri) =>
            !string.IsNullOrWhiteSpace(giaTri) && MauEmail.IsMatch(giaTri.Trim());

        public static bool LaSoDienThoai(string? giaTri) =>
            !string.IsNullOrWhiteSpace(giaTri) && MauSoDienThoai.IsMatch(giaTri.Trim());

        // Chuỗi rỗng thành null để chỉ mục duy nhất có lọc không coi "" là một giá trị
        public static string? ChuanHoa(string? giaTri) =>
            string.IsNullOrWhiteSpace(giaTri) ? null : giaTri.Trim();

        // Tìm tài khoản theo email hoặc số điện thoại người dùng gõ vào
        public static IQueryable<TaiKhoan> TheoDinhDanh(this IQueryable<TaiKhoan> ds, string giaTri)
        {
            var g = giaTri.Trim();
            return ds.Where(x => x.Email == g || x.SoDienThoai == g);
        }

        // Email hoặc số điện thoại đã thuộc tài khoản khác chưa (boQuaId: tài khoản đang sửa)
        public static bool DaDuocDung(this IQueryable<TaiKhoan> ds, string? email, string? soDienThoai, int boQuaId = 0)
        {
            email = ChuanHoa(email);
            soDienThoai = ChuanHoa(soDienThoai);
            return ds.Any(x => x.Id != boQuaId &&
                ((email != null && x.Email == email) || (soDienThoai != null && x.SoDienThoai == soDienThoai)));
        }

        // Giá trị ghi kèm vào cột cũ TaiKhoanDangNhap (ưu tiên email)
        public static string GiaTriCot(string? email, string? soDienThoai) =>
            ChuanHoa(email) ?? ChuanHoa(soDienThoai) ?? string.Empty;

        // Hiển thị ở các bảng admin
        public static string HienThi(TaiKhoan? tk) =>
            tk == null ? string.Empty
            : !string.IsNullOrWhiteSpace(tk.Email) && !string.IsNullOrWhiteSpace(tk.SoDienThoai) ? $"{tk.Email} / {tk.SoDienThoai}"
            : tk.Email ?? tk.SoDienThoai ?? string.Empty;
    }
}
