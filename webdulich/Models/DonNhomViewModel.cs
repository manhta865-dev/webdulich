using System.ComponentModel.DataAnnotations;

namespace webdulich.Models
{
    // Form tạo đơn nhóm (bước 1) và bản nháp giữ trong Session cho tới khi bấm giữ chỗ
    public class TaoDonNhomViewModel
    {
        public int TourId { get; set; }
        public int LichId { get; set; }

        [Range(1, 45, ErrorMessage = "Cần ít nhất 1 người lớn.")]
        public int SoNguoiLon { get; set; } = 2;

        [Range(0, 45, ErrorMessage = "Số trẻ nhỏ không hợp lệ.")]
        public int SoTreNho { get; set; }

        // Trưởng nhóm có đi cùng không; có thì ghế đầu tiên là của trưởng nhóm
        public bool ToiDi { get; set; } = true;

        [Required(ErrorMessage = "Vui lòng nhập tên người liên hệ.")]
        public string TenKhach { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string DienThoai { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? GhiChu { get; set; }

        // Chỉ để hiển thị
        public string TenTour { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
        public DateTime NgayDi { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public decimal GiaNguoiLon { get; set; }
        public decimal GiaTreEm { get; set; }
        public int SoChoCon { get; set; }
        public bool CoSoDo { get; set; }
        public int SoGioGiuCho { get; set; }
    }

    // Trang đơn nhóm của trưởng nhóm
    public class DonNhomChiTietViewModel
    {
        public DonDatTourDoc Don { get; set; } = new DonDatTourDoc();
        public DateTime? HanGiuCho { get; set; }
        public bool DaGiaHan { get; set; }
        public string? LinkMoi { get; set; }
        public List<GheNhomHienThi> DsGhe { get; set; } = new List<GheNhomHienThi>();
        public int SoGheDaNhan => DsGhe.Count(g => g.DaCoChu);
        public int SoGheDaTra => DsGhe.Count(g => g.TrangThai == TrangThaiGhe.DaTra);
    }

    // Một ghế trên bảng tiến độ. Không có số giấy tờ của người khác (RB-20).
    public class GheNhomHienThi
    {
        public int Id { get; set; }
        public int ThuTu { get; set; }
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public decimal GiaLucDat { get; set; }
        public string? MaThanhToan { get; set; }
        public string? TenChu { get; set; }
        public bool LaCuaToi { get; set; }
        public bool DaCoChu { get; set; }
        public string? MaCho { get; set; }
        public string TrangThai { get; set; } = TrangThaiGhe.ChuaNhan;
    }

    // Trạng thái ghế: tính từ dữ liệu, không lưu cột (docs/MO_HINH_DU_LIEU.md, mục 4)
    public static class TrangThaiGhe
    {
        public const string ChuaNhan = "Chưa có người nhận";
        public const string DaNhan = "Đã nhận, chưa điền thông tin";
        public const string DaDien = "Đã điền, chưa trả";
        public const string ChoXacNhan = "Chờ xác nhận thanh toán";
        public const string DaTra = "Đã trả";
        public const string DaNha = "Đã nhả";

        public static string Tinh(HanhKhachDatTour ghe, IEnumerable<GiaoDichThanhToan> dsGiaoDich)
        {
            if (ghe.NgayNha.HasValue) return DaNha;
            var cuaGhe = dsGiaoDich.Where(g => g.HanhKhachDatTourId == ghe.Id).ToList();
            if (cuaGhe.Any(g => g.TrangThai == "ThanhCong")) return DaTra;
            if (cuaGhe.Any(g => g.TrangThai == "ChoXacNhan")) return ChoXacNhan;
            bool daDien = !string.IsNullOrWhiteSpace(ghe.HoTen) && !string.IsNullOrWhiteSpace(ghe.SoGiayTo);
            if (daDien) return DaDien;
            return ghe.TaiKhoanId.HasValue ? DaNhan : ChuaNhan;
        }
    }
}
