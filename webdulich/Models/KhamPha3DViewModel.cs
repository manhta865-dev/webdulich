namespace webdulich.Models
{
    // Trang Khám phá 3D của một tour (DiaDiem/KhamPha): chỉ đọc, dựng từ tọa độ xuất phát và tọa độ từng ngày trong lịch trình
    public class KhamPha3DViewModel
    {
        public int TourId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public decimal ViXuatPhat { get; set; }
        public decimal KinhXuatPhat { get; set; }
        public List<DiemKhamPha3D> DsNgay { get; set; } = new();
        // Google Maps chỉ đường cả hành trình, đi qua điểm của mọi ngày
        public string LinkCaHanhTrinh { get; set; } = string.Empty;
    }

    public class DiemKhamPha3D
    {
        public int Ngay { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string? BuaAn { get; set; }
        public decimal Vi { get; set; }
        public decimal Kinh { get; set; }
        // Đường xe chạy đã lưu (mảng [kinh độ, vĩ độ]); null thì nối thẳng
        public string? DuongDi { get; set; }
        // Google Maps chỉ đường từ điểm trước (ngày 1: điểm xuất phát) tới điểm của ngày này
        public string LinkChiDuong { get; set; } = string.Empty;
        public string LinkStreetView { get; set; } = string.Empty;
        public string LinkNhung { get; set; } = string.Empty;
    }
}
