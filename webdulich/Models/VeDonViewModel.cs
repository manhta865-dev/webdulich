namespace webdulich.Models
{
    // Trang vé của đơn đặt riêng đã thanh toán (DatTour/Ve): mỗi hành khách một tấm vé.
    // Chỉ là thông tin chuyến đi để lưu hoặc gửi cho người đi cùng: không có số giấy tờ, không dùng để soát vé.
    public class VeDonViewModel
    {
        public int DonId { get; set; }
        public string MaChuyenDi { get; set; } = string.Empty;
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public bool CoSoDoChoNgoi { get; set; }
        public List<VeHanhKhach> DanhSachVe { get; set; } = new();
    }

    public class VeHanhKhach
    {
        public int ThuTu { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? MaCho { get; set; }
    }
}
