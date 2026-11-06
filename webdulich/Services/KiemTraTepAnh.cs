namespace webdulich.Services
{
    // Chỉ nhận ảnh thật: xem mấy byte đầu của tệp, không tin tên tệp và đuôi tệp người dùng gửi.
    // Đuôi tệp lưu xuống lấy từ nội dung thật, nên không đưa được tệp .html hay .aspx vào wwwroot.
    public static class KiemTraTepAnh
    {
        public static bool HopLe(IFormFile tep, long toiDaMb, out string? duoi, out string loi)
        {
            duoi = null;
            loi = string.Empty;
            if (tep.Length == 0)
            {
                loi = "Tệp ảnh trống, hãy chọn tệp khác.";
                return false;
            }
            if (tep.Length > toiDaMb * 1024 * 1024)
            {
                loi = $"Ảnh quá lớn, tối đa {toiDaMb} MB.";
                return false;
            }

            var dau = new byte[12];
            using (var luong = tep.OpenReadStream())
            {
                luong.ReadExactly(dau, 0, (int)Math.Min(tep.Length, dau.Length));
            }

            bool laJpg = dau[0] == 0xFF && dau[1] == 0xD8 && dau[2] == 0xFF;
            bool laPng = dau[0] == 0x89 && dau[1] == 0x50 && dau[2] == 0x4E && dau[3] == 0x47;
            bool laWebp = dau[0] == 0x52 && dau[1] == 0x49 && dau[2] == 0x46 && dau[3] == 0x46
                && dau[8] == 0x57 && dau[9] == 0x45 && dau[10] == 0x42 && dau[11] == 0x50;

            duoi = laJpg ? ".jpg" : laPng ? ".png" : laWebp ? ".webp" : null;
            if (duoi == null)
            {
                loi = "Chỉ nhận ảnh JPG, PNG hoặc WebP.";
                return false;
            }
            return true;
        }
    }
}
