using System.ComponentModel.DataAnnotations.Schema;

namespace webdulich.Models
{
    // Chỗ ngồi nào của chuyến nào do ghế nào giữ.
    // Khóa chính ghép (LichKhoiHanhTourId, ViTriChoNgoiId) để CSDL tự chặn hai người chọn cùng một chỗ.
    public class ChoNgoiChuyenDi
    {
        public int LichKhoiHanhTourId { get; set; }

        public int ViTriChoNgoiId { get; set; }

        public int HanhKhachDatTourId { get; set; }

        [ForeignKey("LichKhoiHanhTourId")]
        public LichKhoiHanhTour? LichKhoiHanhTour { get; set; }

        [ForeignKey("ViTriChoNgoiId")]
        public ViTriChoNgoi? ViTriChoNgoi { get; set; }

        [ForeignKey("HanhKhachDatTourId")]
        public HanhKhachDatTour? HanhKhachDatTour { get; set; }
    }
}
