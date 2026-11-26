namespace webdulich.Models
{
    // Admin đặt tour hộ khách tại quầy (RB-42): tạo đơn cá nhân đã xác nhận kèm giao dịch tiền mặt tại quầy,
    // trừ chỗ và khóa chỗ ngồi trong cùng một giao dịch CSDL như đặt online.
    public class DatTaiQuayViewModel
    {
        public int LichId { get; set; }
        public int TourId { get; set; }

        public int SoNguoiLon { get; set; } = 1;
        public int SoTreNho { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tên người liên hệ.")]
        [System.ComponentModel.DataAnnotations.StringLength(100, ErrorMessage = "Tên người liên hệ tối đa 100 ký tự.")]
        public string TenKhach { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [System.ComponentModel.DataAnnotations.StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string DienThoai { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? GhiChu { get; set; }

        // Họ tên từng hành khách (không bắt buộc, điền sau ở trang chi tiết đơn được)
        public List<string?> HoTen { get; set; } = new();

        // Chỉ để hiển thị
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public int SoChoCon { get; set; }
        public decimal GiaNguoiLon { get; set; }
        public decimal GiaTreEm { get; set; }
        public decimal TongTien => SoNguoiLon * GiaNguoiLon + SoTreNho * GiaTreEm;
        public ChonChoNgoiViewModel? SoDo { get; set; }
    }
}
