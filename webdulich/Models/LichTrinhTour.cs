using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    public class LichTrinhTour
    {
        [Key]
        public int Id { get; set; }

        public int TourId { get; set; }

        public int NgayThu { get; set; }

        [Required]
        public string TieuDe { get; set; } = string.Empty;

        public string? BuaAn { get; set; }

        public string? NoiDung { get; set; }

        // Nơi xe tới vào cuối ngày, dùng cho bản đồ hành trình. Hai cột cùng có hoặc cùng rỗng (CK_LichTrinh_ToaDo).
        public decimal? ViDo { get; set; }

        public decimal? KinhDo { get; set; }

        // Đường xe chạy từ điểm cuối ngày trước (hoặc điểm xuất phát) tới điểm của ngày này, dạng JSON [[kinh độ, vĩ độ], ...].
        // Bộ nhớ đệm có chủ đích: tính được từ tọa độ, lấy một lần lúc admin lưu để khách xem không phụ thuộc dịch vụ ngoài.
        // Rỗng nghĩa là chưa lấy được, bản đồ nối tạm bằng nét đứt thẳng.
        public string? DuongDi { get; set; }

        [ForeignKey("TourId")]
        public Tour? Tour { get; set; }

        // Các điểm tham quan của ngày này theo thứ tự ghé (Mo23)
        public ICollection<LichTrinhDiemThamQuan> DsDiemThamQuan { get; set; } = new List<LichTrinhDiemThamQuan>();

        // Khung kiểm tra thô quanh Việt Nam để bắt lỗi nhập nhầm, đảo vĩ độ với kinh độ. Trùng với CK_LichTrinh_ToaDo.
        public const decimal ViDoNhoNhat = 8m, ViDoLonNhat = 24m, KinhDoNhoNhat = 102m, KinhDoLonNhat = 110m;

        // Đọc tọa độ dán từ Google Maps, dạng "11.9404, 108.4583". Ô trống nghĩa là chưa có tọa độ.
        public static bool DocToaDo(string? chu, out decimal? viDo, out decimal? kinhDo)
        {
            viDo = null;
            kinhDo = null;
            if (string.IsNullOrWhiteSpace(chu))
            {
                return true;
            }

            var phan = chu.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var soThuc = System.Globalization.CultureInfo.InvariantCulture;
            var kieu = System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint;
            if (phan.Length != 2
                || !decimal.TryParse(phan[0], kieu, soThuc, out var vi)
                || !decimal.TryParse(phan[1], kieu, soThuc, out var kinh)
                || vi < ViDoNhoNhat || vi > ViDoLonNhat
                || kinh < KinhDoNhoNhat || kinh > KinhDoLonNhat)
            {
                return false;
            }

            viDo = Math.Round(vi, 6);
            kinhDo = Math.Round(kinh, 6);
            return true;
        }

        // Giá trị hiện lại trong ô nhập, luôn dùng dấu chấm thập phân
        public string ToaDoDangChu => ViDo.HasValue && KinhDo.HasValue
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{ViDo.Value:0.######}, {KinhDo.Value:0.######}")
            : string.Empty;
    }
}