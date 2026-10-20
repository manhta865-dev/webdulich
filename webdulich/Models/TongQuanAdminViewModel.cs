namespace webdulich.Models
{
    // Dữ liệu cho trang Tổng quan của admin (chỉ đọc, không lưu xuống database).
    public class TongQuanAdminViewModel
    {
        public int Thang { get; set; }
        public int Nam { get; set; }

        public decimal DoanhThuThang { get; set; }
        public int SoDonDaThanhToanThang { get; set; }
        public int SoDonChoXacNhan { get; set; }
        public int SoLichSapKhoiHanh { get; set; }
        public int SoTour { get; set; }
        public int TinNhanChuaDoc { get; set; }

        // 12 phần tử: doanh thu từng tháng của năm hiện tại (đơn "Đã thanh toán", tính theo ngày đi)
        public List<decimal> DoanhThu12Thang { get; set; } = new List<decimal>();

        public List<DonChoXacNhanItem> DonChoXacNhan { get; set; } = new List<DonChoXacNhanItem>();
        public List<LichSapKhoiHanhItem> LichSapKhoiHanh { get; set; } = new List<LichSapKhoiHanhItem>();
        public int SoTourThieuLich { get; set; }
        public List<TourThieuLichItem> TourThieuLich { get; set; } = new List<TourThieuLichItem>();
    }

    public class DonChoXacNhanItem
    {
        public int Id { get; set; }
        public string TenKhach { get; set; } = string.Empty;
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public decimal TongTien { get; set; }
        public string PhuongThucThanhToan { get; set; } = string.Empty;
    }

    public class LichSapKhoiHanhItem
    {
        public int Id { get; set; }
        public int TourId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayKhoiHanh { get; set; }
        public int SoChoCon { get; set; }
    }

    public class TourThieuLichItem
    {
        public int Id { get; set; }
        public string TenTour { get; set; } = string.Empty;
    }
}
