namespace webdulich.Models
{
    // Trang admin xem chi tiết một đơn: thông tin đọc sẵn, ghế, giao dịch và giá để đổi số người
    public class ChiTietDonViewModel
    {
        public DonDatTourDoc Don { get; set; } = new DonDatTourDoc();
        public List<HanhKhachDatTour> DsGhe { get; set; } = new List<HanhKhachDatTour>();
        public List<GiaoDichThanhToan> DsGiaoDich { get; set; } = new List<GiaoDichThanhToan>();

        // Mã chỗ ngồi theo Id ghế (ghế chưa chọn chỗ thì không có trong từ điển)
        public Dictionary<int, string> ChoTheoGhe { get; set; } = new Dictionary<int, string>();
        public decimal GiaNguoiLon { get; set; }
        public decimal GiaTreEm { get; set; }
        public int SoChoCon { get; set; }

        // Chỉ đổi số người khi đơn chưa hủy, chưa thanh toán và lịch chưa khởi hành (đơn nhóm thì không)
        public bool DuocDoiSoNguoi { get; set; }

        // Đơn nhóm (N6): hạn giữ chỗ, gia hạn, trạng thái từng ghế (tính từ giao dịch, không lưu)
        public DateTime? HanGiuCho { get; set; }
        public bool DaGiaHan { get; set; }
        public Dictionary<int, string> TrangThaiTheoGhe { get; set; } = new Dictionary<int, string>();
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
