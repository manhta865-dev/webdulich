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

        // So với tháng trước (cùng cách tính: đơn đã thanh toán, theo ngày đi)
        public decimal DoanhThuThangTruoc { get; set; }
        // Phần trăm tăng (dương) hoặc giảm (âm); null khi tháng trước chưa có doanh thu
        public decimal? PhanTramSoVoiThangTruoc =>
            DoanhThuThangTruoc > 0 ? Math.Round((DoanhThuThang - DoanhThuThangTruoc) * 100m / DoanhThuThangTruoc, 1) : null;

        // Số đơn đặt trong 30 ngày qua theo trạng thái thanh toán (tính, không lưu)
        public int DonDaThanhToan30Ngay { get; set; }
        public int DonDangKiemTra30Ngay { get; set; }
        public int DonChuaThanhToan30Ngay { get; set; }
        public int DonDaHuy30Ngay { get; set; }
        public int TongDon30Ngay => DonDaThanhToan30Ngay + DonDangKiemTra30Ngay + DonChuaThanhToan30Ngay + DonDaHuy30Ngay;
        public List<TourThieuLichItem> TourThieuLich { get; set; } = new List<TourThieuLichItem>();

        // Tên người đang xem (lời chào đầu trang)
        public string TenNguoiXem { get; set; } = string.Empty;

        // Lấp đầy chỗ của mọi lịch chưa hủy trong 30 ngày tới (chỗ mở bán theo RB-37, tính, không lưu)
        public int SoLichTinhLapDay { get; set; }
        public int TongChoMoBan30Ngay { get; set; }
        public int TongChoDaDat30Ngay { get; set; }
        public int PhanTramLapDay => TongChoMoBan30Ngay > 0 ? (int)Math.Round(TongChoDaDat30Ngay * 100m / TongChoMoBan30Ngay) : 0;

        // 10 đơn đặt gần nhất (không có số giấy tờ hay liên hệ của khách)
        public List<DonGanDayItem> DonGanDay { get; set; } = new List<DonGanDayItem>();
    }

    public class DonGanDayItem
    {
        public int Id { get; set; }
        public string MaChuyenDi { get; set; } = string.Empty;
        public string TenTour { get; set; } = string.Empty;
        public string TenKhach { get; set; } = string.Empty;
        public bool LaDonNhom { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThaiThanhToan { get; set; } = string.Empty;
        public DateTime NgayDat { get; set; }
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
        // Số chỗ mở bán (RB-37) để hiện "đã đặt X/Y"
        public int SoChoMoBan { get; set; }
        public int SoChoDaDat => Math.Max(0, SoChoMoBan - SoChoCon);
    }

    public class TourThieuLichItem
    {
        public int Id { get; set; }
        public string TenTour { get; set; } = string.Empty;
    }
}
