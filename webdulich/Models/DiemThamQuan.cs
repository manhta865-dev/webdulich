using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Điểm tham quan trong một điểm đến (Mo23, RB-49), ví dụ Hồ Xuân Hương, Langbiang thuộc Đà Lạt.
    // Tên và tọa độ là thông tin của chính điểm, nên tách bảng riêng: nhiều tour, nhiều ngày ghé cùng một điểm thì dùng chung một dòng.
    // Dùng cho chip địa điểm, ghim trên bản đồ 3D và Street View ở trang Khám phá 3D.
    public class DiemThamQuan
    {
        [Key]
        public int Id { get; set; }

        // Điểm đến chứa điểm này; điểm đang có ở điểm đến thì không xóa được điểm đến (Restrict)
        public int DiaDiemId { get; set; }

        [Required, StringLength(150)]
        public string Ten { get; set; } = string.Empty;

        // Bắt buộc, trong khung Việt Nam như tọa độ lịch trình (CK_DiemThamQuan_ToaDo)
        public decimal ViDo { get; set; }

        public decimal KinhDo { get; set; }

        // Địa chỉ nhúng đúng tấm ảnh 360 của điểm (Mo24), tách từ mã "Nhúng bản đồ" của Google Maps; luôn bắt đầu bằng
        // https://www.google.com/maps/embed? (CK_DiemThamQuan_LinkNhung360). Rỗng: Street View tự chọn ảnh đường phố gần tọa độ nhất.
        [StringLength(1000)]
        public string? LinkNhung360 { get; set; }

        [ForeignKey("DiaDiemId")]
        public DiaDiem? DiaDiem { get; set; }

        public ICollection<LichTrinhDiemThamQuan> DsLichTrinh { get; set; } = new List<LichTrinhDiemThamQuan>();
    }

    // Ngày nào của lịch trình ghé điểm nào, theo thứ tự (Mo23). Khóa chính là cặp (ngày, điểm): một ngày không ghé một điểm hai lần.
    // Thứ tự ghé là thông tin của cặp nên nằm ở bảng nối, không nằm ở bảng điểm.
    public class LichTrinhDiemThamQuan
    {
        public int LichTrinhTourId { get; set; }

        public int DiemThamQuanId { get; set; }

        // 1, 2, 3... trong ngày; không trùng trong cùng một ngày (UNIQUE)
        public int ThuTu { get; set; }

        [ForeignKey("LichTrinhTourId")]
        public LichTrinhTour? LichTrinhTour { get; set; }

        [ForeignKey("DiemThamQuanId")]
        public DiemThamQuan? DiemThamQuan { get; set; }
    }
}
