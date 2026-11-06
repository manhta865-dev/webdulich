using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace webdulich.Models
{
    // Bài viết du lịch. Tên tác giả và tên tour lấy qua khóa ngoại, không lưu lại (3NF).
    public class BaiViet
    {
        public const string Nhap = "Nhap";
        public const string DaDang = "DaDang";

        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        // Địa chỉ trang, ví dụ /BaiViet/kinh-nghiem-di-da-lat. Đặt một lần lúc tạo, sửa tiêu đề không đổi để link đã chia sẻ vẫn dùng được.
        [MaxLength(220)]
        public string DuongDan { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TomTat { get; set; } = string.Empty;

        // Chữ thuần, không lưu HTML nên không chèn được mã độc. Dòng trống xuống đoạn, dòng bắt đầu bằng "## " là tiêu đề nhỏ.
        public string NoiDung { get; set; } = string.Empty;

        [MaxLength(260)]
        public string? AnhBia { get; set; }

        [MaxLength(10)]
        public string TrangThai { get; set; } = Nhap;

        public DateTime NgayTao { get; set; }

        // Lần đăng gần nhất; bài đã đăng thì bắt buộc có (CK_BaiViet_NgayDang)
        public DateTime? NgayDang { get; set; }

        public int TacGiaId { get; set; }

        // Tour liên quan, có thể không có. Xóa tour thì bài vẫn còn.
        public int? TourId { get; set; }

        [ForeignKey("TacGiaId")]
        public TaiKhoan? TacGia { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        // Tiêu đề thành đường dẫn: bỏ dấu tiếng Việt, chữ thường, các từ nối bằng dấu gạch ngang
        public static string TaoDuongDan(string tieuDe)
        {
            var khongDau = new StringBuilder();
            foreach (var kyTu in tieuDe.Replace('đ', 'd').Replace('Đ', 'd').Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(kyTu) != UnicodeCategory.NonSpacingMark)
                {
                    khongDau.Append(char.ToLowerInvariant(kyTu));
                }
            }

            var ketQua = new StringBuilder();
            foreach (var kyTu in khongDau.ToString())
            {
                if (char.IsAsciiLetterOrDigit(kyTu))
                {
                    ketQua.Append(kyTu);
                }
                else if (ketQua.Length > 0 && ketQua[^1] != '-')
                {
                    ketQua.Append('-');
                }
            }

            var duongDan = ketQua.ToString().Trim('-');
            if (duongDan.Length > 200)
            {
                duongDan = duongDan[..200].TrimEnd('-');
            }
            return duongDan.Length == 0 ? "bai-viet" : duongDan;
        }

        // Tách nội dung thành các khối để hiển thị. Razor tự mã hóa chữ nên nội dung không chạy được như HTML.
        public static List<KhoiNoiDung> TachKhoi(string noiDung)
        {
            var dsKhoi = new List<KhoiNoiDung>();
            var doan = new List<string>();

            void KetThucDoan()
            {
                if (doan.Count > 0)
                {
                    dsKhoi.Add(new KhoiNoiDung(false, string.Join("\n", doan)));
                    doan.Clear();
                }
            }

            foreach (var dong in noiDung.Replace("\r\n", "\n").Split('\n'))
            {
                var chu = dong.Trim();
                if (chu.Length == 0)
                {
                    KetThucDoan();
                }
                else if (chu.StartsWith("## "))
                {
                    KetThucDoan();
                    dsKhoi.Add(new KhoiNoiDung(true, chu[3..].Trim()));
                }
                else
                {
                    doan.Add(chu);
                }
            }
            KetThucDoan();
            return dsKhoi;
        }
    }

    public record KhoiNoiDung(bool LaTieuDe, string Chu);

    // Form viết và sửa bài của admin
    public class BaiVietForm
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Nhập tiêu đề bài viết.")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự.")]
        public string TieuDe { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập vài câu tóm tắt để hiện ở danh sách bài viết.")]
        [StringLength(500, ErrorMessage = "Tóm tắt tối đa 500 ký tự.")]
        public string TomTat { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nhập nội dung bài viết.")]
        public string NoiDung { get; set; } = string.Empty;

        public int? TourId { get; set; }

        public IFormFile? TepAnhBia { get; set; }

        public bool XoaAnhBia { get; set; }

        // Chỉ để hiện ảnh đang có khi sửa
        public string? AnhBiaHienTai { get; set; }

        public string TrangThaiHienTai { get; set; } = BaiViet.Nhap;
    }
}
