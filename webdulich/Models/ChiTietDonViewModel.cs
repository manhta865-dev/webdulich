namespace webdulich.Models
{
    // Trang admin xem chi tiết một đơn: thông tin đọc sẵn, ghế, giao dịch và giá để đổi số người
    public class ChiTietDonViewModel
    {
        public DonDatTourDoc Don { get; set; } = new DonDatTourDoc();
        public List<HanhKhachDatTour> DsGhe { get; set; } = new List<HanhKhachDatTour>();
        public List<GiaoDichThanhToan> DsGiaoDich { get; set; } = new List<GiaoDichThanhToan>();
        public decimal GiaNguoiLon { get; set; }
        public decimal GiaTreEm { get; set; }
        public int SoChoCon { get; set; }

        // Chỉ đổi số người khi đơn chưa hủy, chưa thanh toán và lịch chưa khởi hành
        public bool DuocDoiSoNguoi { get; set; }
    }

    // Phần admin được sửa trực tiếp trên đơn: thông tin liên hệ
    public class SuaLienHeDonModel
    {
        public int Id { get; set; }
        public string TenKhach { get; set; } = string.Empty;
        public string DienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DiaChi { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }
}
