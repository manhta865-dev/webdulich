using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace webdulich.Models
{
    // Video giới thiệu, lưu mã YouTube chứ không lưu tệp video vì host dùng chung không phù hợp để phát video nặng.
    public class Video
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        // Mã 11 ký tự của YouTube, một video chỉ thêm một lần
        [MaxLength(11)]
        public string MaYouTube { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? MoTa { get; set; }

        // Số nhỏ hiện trước
        public int ThuTu { get; set; }

        public bool DangHien { get; set; } = true;

        public int? TourId { get; set; }

        public DateTime NgayTao { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        public string LinkXem => "https://www.youtube.com/watch?v=" + MaYouTube;

        public string AnhXemTruoc => "https://i.ytimg.com/vi/" + MaYouTube + "/hqdefault.jpg";

        private static readonly Regex KieuMa = new("^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);

        // Lấy mã video từ link YouTube. Chỉ nhận đúng các địa chỉ của YouTube, trả về null nếu không phải.
        public static string? DocMaYouTube(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return null;
            }

            var chu = link.Trim();
            if (!chu.Contains("://"))
            {
                chu = "https://" + chu;
            }
            if (!Uri.TryCreate(chu, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            {
                return null;
            }

            var phan = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string? ma = null;
            switch (uri.Host.ToLowerInvariant())
            {
                case "youtu.be":
                    ma = phan.FirstOrDefault();
                    break;
                case "youtube.com":
                case "www.youtube.com":
                case "m.youtube.com":
                case "www.youtube-nocookie.com":
                    if (phan.Length == 1 && phan[0] == "watch")
                    {
                        ma = uri.Query.TrimStart('?').Split('&')
                            .FirstOrDefault(x => x.StartsWith("v="))?[2..];
                    }
                    else if (phan.Length >= 2 && phan[0] is "shorts" or "embed" or "live")
                    {
                        ma = phan[1];
                    }
                    break;
            }
            return ma != null && KieuMa.IsMatch(ma) ? ma : null;
        }
    }

    // Form thêm và sửa video của admin
    public class VideoForm
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Dán link video YouTube.")]
        [StringLength(300, ErrorMessage = "Link quá dài, hãy kiểm tra lại.")]
        public string Link { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập tiêu đề video.")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
        public string TieuDe { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        public string? MoTa { get; set; }

        public int? TourId { get; set; }

        public bool DangHien { get; set; } = true;
    }
}
