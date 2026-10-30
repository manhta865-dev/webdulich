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
        public bool CoTaiKhoan { get; set; }
        public string? MaCho { get; set; }
        public string TrangThai { get; set; } = TrangThaiGhe.ChuaNhan;

        // Trưởng nhóm mở được ghế của mình và ghế không có tài khoản (điền hộ trẻ nhỏ, ông bà), không mở ghế người khác (RB-20)
        public bool TruongNhomMoDuoc => TrangThai != TrangThaiGhe.DaNha && (LaCuaToi || !CoTaiKhoan);

        // Ghế thành viên khác đã điền xong, chưa trả: trưởng nhóm trả hộ được qua trang riêng, không thấy giấy tờ
        public bool TruongNhomTraHoDuoc => !TruongNhomMoDuoc && TrangThai == TrangThaiGhe.DaDien;
    }

    // Trang link mời. Ai có link cũng xem được nên không có tên người đã nhận ghế (quy tắc N3).
    public class MoiNhomViewModel
    {
        public string Ma { get; set; } = string.Empty;
        public string MaChuyenDi { get; set; } = string.Empty;
        public string TenTour { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
        public DateTime NgayDi { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public string TenTruongNhom { get; set; } = string.Empty;
        public DateTime HanGiuCho { get; set; }
        public bool DaGiaHan { get; set; }
        public bool DaDangNhap { get; set; }
        public bool LaTruongNhom { get; set; }
        public int DonId { get; set; }
        public List<GheMoiHienThi> DsGhe { get; set; } = new List<GheMoiHienThi>();
        public int SoGheTrong => DsGhe.Count(g => g.NhanDuoc);
    }

    public class GheMoiHienThi
    {
        public int Id { get; set; }
        public int ThuTu { get; set; }
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public decimal GiaLucDat { get; set; }
        public string? MaCho { get; set; }
        public bool ConTrong { get; set; }

        // Ghế trẻ nhỏ không nhận qua link: trưởng nhóm điền hộ (quy tắc N3)
        public bool NhanDuoc => ConTrong && LoaiHanhKhach != "Trẻ nhỏ";
    }

    // Thông tin hành khách của một ghế, do chủ ghế hoặc trưởng nhóm (ghế không có tài khoản) điền
    public class ThongTinGheForm
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn loại giấy tờ.")]
        public string LoaiGiayTo { get; set; } = "CCCD";

        [Required(ErrorMessage = "Vui lòng nhập số giấy tờ.")]
        [StringLength(20, ErrorMessage = "Số giấy tờ tối đa 20 ký tự.")]
        public string SoGiayTo { get; set; } = string.Empty;

        public static readonly string[] DsLoaiGiayTo = { "CCCD", "CMND", "Hộ chiếu", "Giấy khai sinh" };
    }

    // Thông tin chuyển khoản cho một ghế: tài khoản nhận lấy từ cấu hình "ThanhToan", số tiền và nội dung riêng của ghế
    public class ThongTinChuyenKhoan
    {
        public string MaNganHang { get; set; } = string.Empty;
        public string SoTaiKhoan { get; set; } = string.Empty;
        public string TenTaiKhoan { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public decimal SoTien { get; set; }
        public string NoiDung { get; set; } = string.Empty;

        // Ảnh QR theo chuẩn VietQR: app ngân hàng quét là điền sẵn số tiền và nội dung
        public string LinkQr =>
            $"https://img.vietqr.io/image/{Uri.EscapeDataString(MaNganHang)}-{Uri.EscapeDataString(SoTaiKhoan)}-compact2.png"
            + $"?amount={decimal.ToInt64(SoTien)}&addInfo={Uri.EscapeDataString(NoiDung)}&accountName={Uri.EscapeDataString(TenTaiKhoan)}";

        public static ThongTinChuyenKhoan Tao(IConfiguration cauHinh, decimal soTien, string noiDung) => new ThongTinChuyenKhoan
        {
            MaNganHang = cauHinh["ThanhToan:MaNganHang"] ?? string.Empty,
            SoTaiKhoan = cauHinh["ThanhToan:SoTaiKhoan"] ?? string.Empty,
            TenTaiKhoan = cauHinh["ThanhToan:TenTaiKhoan"] ?? string.Empty,
            GhiChu = cauHinh["ThanhToan:GhiChu"],
            SoTien = soTien,
            NoiDung = noiDung
        };
    }

    // Khung trả tiền của một ghế, dùng chung cho trang "Ghế của tôi" và trang trả hộ của trưởng nhóm
    public class TraTienGheViewModel
    {
        public int GheId { get; set; }
        public string TrangThai { get; set; } = TrangThaiGhe.ChuaNhan;
        public decimal GiaLucDat { get; set; }
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public string? MaThanhToan { get; set; }

        // Có QR và nút báo đã chuyển khi ghế đã điền thông tin, chưa trả, và đơn còn giữ chỗ
        public bool DuocTra { get; set; }

        // Người đang xem trả cho ghế không phải của mình (trưởng nhóm trả hộ)
        public bool TraHo { get; set; }
        public ThongTinChuyenKhoan? ChuyenKhoan { get; set; }
    }

    // Trang trả hộ: trưởng nhóm thấy tên, giá và QR của ghế, không thấy giấy tờ (RB-20)
    public class TraHoViewModel
    {
        public int DonId { get; set; }
        public string MaChuyenDi { get; set; } = string.Empty;
        public string TenTour { get; set; } = string.Empty;
        public int ThuTu { get; set; }
        public string? TenChu { get; set; }
        public TraTienGheViewModel TraTien { get; set; } = new TraTienGheViewModel();
    }

    // Trang một ghế: thông tin, chỗ ngồi, trả lại ghế, trả tiền
    public class GheCuaToiViewModel
    {
        public int GheId { get; set; }
        public int DonId { get; set; }
        public int ThuTu { get; set; }
        public string MaChuyenDi { get; set; } = string.Empty;
        public string? MaThanhToan { get; set; }
        public string? MaMoi { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public DateTime? HanGiuCho { get; set; }
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public decimal GiaLucDat { get; set; }
        public string? MaCho { get; set; }
        public string TrangThai { get; set; } = TrangThaiGhe.ChuaNhan;

        // Trưởng nhóm đang điền hộ ghế không có tài khoản
        public bool DienHo { get; set; }
        public bool LaTruongNhom { get; set; }
        public bool DuocSua { get; set; }
        public bool DuocDoiCho { get; set; }
        public bool CoSoDo { get; set; }
        public bool DaDien { get; set; }
        public ThongTinGheForm Form { get; set; } = new ThongTinGheForm();
        public TraTienGheViewModel TraTien { get; set; } = new TraTienGheViewModel();
    }

    // Ghế mình nhận trong đơn nhóm của người khác, hiện ở Lịch sử
    public class GheNhomTrongLichSu
    {
        public int GheId { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public string MaChuyenDi { get; set; } = string.Empty;
        public string TenTruongNhom { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public decimal GiaLucDat { get; set; }
        public string? MaCho { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public bool DonDaHuy { get; set; }
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
