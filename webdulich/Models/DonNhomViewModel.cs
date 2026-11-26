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
        [StringLength(100, ErrorMessage = "Tên người liên hệ tối đa 100 ký tự.")]
        public string TenKhach { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        public string DienThoai { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? GhiChu { get; set; }

        // Mã khuyến mãi (không bắt buộc). Tiền giảm chia cho từng ghế theo tỷ lệ giá (RB-40)
        public string? MaKhuyenMai { get; set; }

        // Chỉ để hiển thị
        public string? TenKhuyenMai { get; set; }
        public decimal TienGiamDuKien { get; set; }
        public string TenTour { get; set; } = string.Empty;
        public string? HinhAnh { get; set; }
        public DateTime NgayDi { get; set; }
        public TimeSpan? GioKhoiHanh { get; set; }
        public decimal GiaNguoiLon { get; set; }
        public decimal GiaTreEm { get; set; }
        public int SoChoCon { get; set; }
        public bool CoSoDo { get; set; }
        public int SoGioGiuCho { get; set; }

        // Hạn giữ chỗ nếu tạo đơn bây giờ: 24 giờ, nhưng không quá 23:59:59 hôm trước ngày đi
        public DateTime HanGiuChoDuKien { get; set; }

        // Hạn bị rút ngắn vì sát ngày đi (ngắn hơn số giờ cấu hình)
        public bool HanBiRutNgan { get; set; }
    }

    // Trang đơn nhóm của trưởng nhóm
    public class DonNhomChiTietViewModel
    {
        public DonDatTourDoc Don { get; set; } = new DonDatTourDoc();
        public DateTime? HanGiuCho { get; set; }
        public bool DaGiaHan { get; set; }
        public string? LinkMoi { get; set; }

        // Lý do đơn không còn nhận thay đổi (cùng điều kiện với máy chủ); null là còn nhận
        public string? LyDoKhongNhan { get; set; }
        public bool ConNhanThayDoi => LyDoKhongNhan == null;
        public List<GheNhomHienThi> DsGhe { get; set; } = new List<GheNhomHienThi>();
        public int SoGheDaNhan => DsGhe.Count(g => g.DaCoChu);
        public int SoGheDaTra => DsGhe.Count(g => g.TrangThai == TrangThaiGhe.DaTra);

        // Xe của nhóm: chỉ có khi lịch có sơ đồ chỗ và nhóm đã chọn ít nhất một chỗ
        public SoDoNhom? SoDo { get; set; }
    }

    // Sơ đồ chỗ của cả chuyến, nhìn từ phía trưởng nhóm. Dữ liệu này gửi xuống trình duyệt nên
    // chỗ của khách ngoài nhóm chỉ ghi "khach-khac", không có tên, không có mã ghế.
    public class SoDoNhom
    {
        public string TenSoDo { get; set; } = string.Empty;
        public int SoHang { get; set; }
        public int SoCot { get; set; }
        public bool LaMayBay { get; set; }
        public List<ChoTrenSoDo> DsCho { get; set; } = new List<ChoTrenSoDo>();
    }

    public class ChoTrenSoDo
    {
        public string Ma { get; set; } = string.Empty;
        public int Hang { get; set; }
        public int Cot { get; set; }

        // con-trong, khong-ban (chỗ không mở bán, RB-37), khach-khac, hoặc trạng thái ghế của nhóm: trong, da-nhan, da-dien, cho, da-tra
        public string Loai { get; set; } = "con-trong";

        // Chỉ ghế của nhóm mới có hai giá trị này
        public string? Ten { get; set; }
        public string? TrangThai { get; set; }
    }

    // Một ghế trên bảng tiến độ. Không có số giấy tờ của người khác (RB-20).
    public class GheNhomHienThi
    {
        public int Id { get; set; }
        public int ThuTu { get; set; }
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public decimal GiaLucDat { get; set; }
        // Phần giảm của ghế (đơn nhóm dùng mã, RB-40) và số tiền ghế phải trả
        public decimal TienGiam { get; set; }
        public decimal PhaiTra => GiaLucDat - TienGiam;
        public string? MaThanhToan { get; set; }
        public string? TenChu { get; set; }
        public bool LaCuaToi { get; set; }
        public bool DaCoChu { get; set; }
        public bool CoTaiKhoan { get; set; }
        public string? MaCho { get; set; }
        public string TrangThai { get; set; } = TrangThaiGhe.ChuaNhan;

        // Admin vừa từ chối chuyển khoản của ghế, ghế cần báo chuyển khoản lại
        public bool BiTuChoi { get; set; }

        // Trưởng nhóm mở được ghế của mình và ghế không có tài khoản (điền hộ trẻ nhỏ, ông bà), không mở ghế người khác (RB-20)
        public bool TruongNhomMoDuoc => TrangThai != TrangThaiGhe.DaNha && (LaCuaToi || !CoTaiKhoan);

        // Ghế thành viên khác đã điền xong, chưa trả: trưởng nhóm trả hộ được qua trang riêng, không thấy giấy tờ
        public bool TruongNhomTraHoDuoc => !TruongNhomMoDuoc && TrangThai == TrangThaiGhe.DaDien;

        // Ghế chưa báo chuyển khoản, chưa trả, chưa nhả: trưởng nhóm còn bớt được (N2)
        public bool ChuaTra => TrangThai is TrangThaiGhe.ChuaNhan or TrangThaiGhe.DaNhan or TrangThaiGhe.DaDien;

        // Thành viên khác đã nhận mà chưa trả: trưởng nhóm mời ra được, ghế về trống (N2)
        public bool DuocMoiRa => CoTaiKhoan && !LaCuaToi && ChuaTra;
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
        // Phần giảm của ghế (đơn nhóm dùng mã, RB-40) và số tiền ghế phải trả
        public decimal TienGiam { get; set; }
        public decimal PhaiTra => GiaLucDat - TienGiam;
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
        // Phương thức người trả chọn khi báo đã chuyển, ghi vào GiaoDichThanhToan.PhuongThuc
        public const string NganHang = "Chuyển khoản";
        public const string Momo = "MoMo";

        public string TenNganHang { get; set; } = string.Empty;
        public string MaNganHang { get; set; } = string.Empty;
        public string SoTaiKhoan { get; set; } = string.Empty;
        public string TenTaiKhoan { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public decimal SoTien { get; set; }
        public string NoiDung { get; set; } = string.Empty;

        // Ví MoMo: QR cố định (không tự điền số tiền, nội dung), người trả tự gõ. Để trống trong cấu hình thì ẩn.
        public string? SoMomo { get; set; }
        public string? AnhQrMomo { get; set; }
        public bool CoMomo => !string.IsNullOrEmpty(SoMomo) && !string.IsNullOrEmpty(AnhQrMomo);

        // Ảnh QR theo chuẩn VietQR: app ngân hàng quét là điền sẵn số tiền và nội dung
        public string LinkQr =>
            $"https://img.vietqr.io/image/{Uri.EscapeDataString(MaNganHang)}-{Uri.EscapeDataString(SoTaiKhoan)}-compact2.png"
            + $"?amount={decimal.ToInt64(SoTien)}&addInfo={Uri.EscapeDataString(NoiDung)}&accountName={Uri.EscapeDataString(TenTaiKhoan)}";

        public static ThongTinChuyenKhoan Tao(IConfiguration cauHinh, decimal soTien, string noiDung) => new ThongTinChuyenKhoan
        {
            TenNganHang = cauHinh["ThanhToan:TenNganHang"] ?? string.Empty,
            SoMomo = cauHinh["ThanhToan:SoMomo"],
            AnhQrMomo = cauHinh["ThanhToan:AnhQrMomo"],
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
        // Phần giảm của ghế (đơn nhóm dùng mã, RB-40) và số tiền ghế phải trả
        public decimal TienGiam { get; set; }
        public decimal PhaiTra => GiaLucDat - TienGiam;
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public string? MaThanhToan { get; set; }

        // Có QR và nút báo đã chuyển khi ghế đã điền thông tin, chưa trả, và đơn còn giữ chỗ
        public bool DuocTra { get; set; }

        // Đơn không còn nhận thay đổi thì nói lý do thay cho QR
        public string? LyDoKhongTra { get; set; }

        // Lúc admin từ chối chuyển khoản gần nhất của ghế
        public DateTime? LucBiTuChoi { get; set; }

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

        // Trưởng nhóm đã chốt đơn: chuyến đi chắc chắn với các ghế đã trả (N2)
        public bool DonDaChot { get; set; }

        // Đơn đã dùng lượt gia hạn duy nhất (N5)
        public bool DaGiaHan { get; set; }
        public string LoaiHanhKhach { get; set; } = string.Empty;
        public decimal GiaLucDat { get; set; }
        // Phần giảm của ghế (đơn nhóm dùng mã, RB-40) và số tiền ghế phải trả
        public decimal TienGiam { get; set; }
        public decimal PhaiTra => GiaLucDat - TienGiam;
        public string? MaCho { get; set; }
        public string TrangThai { get; set; } = TrangThaiGhe.ChuaNhan;

        // Trưởng nhóm đang điền hộ ghế không có tài khoản
        public bool DienHo { get; set; }
        public bool LaTruongNhom { get; set; }
        public bool DuocSua { get; set; }
        public string? LyDoKhongSua { get; set; }
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
        // Phần giảm của ghế (đơn nhóm dùng mã, RB-40) và số tiền ghế phải trả
        public decimal TienGiam { get; set; }
        public decimal PhaiTra => GiaLucDat - TienGiam;
        public string? MaCho { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public bool DonDaHuy { get; set; }

        // Ghế bị nhả sau khi chuyển khoản bị từ chối
        public bool BiTuChoi { get; set; }
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

        // Lúc admin từ chối giao dịch gần nhất của ghế; null nếu giao dịch gần nhất không bị từ chối.
        // Hủy đơn cũng đổi giao dịch thành ThatBai nhưng không ghi NgayXacNhan nên không tính ở đây.
        public static DateTime? LucBiTuChoi(int gheId, IEnumerable<GiaoDichThanhToan> dsGiaoDich)
        {
            var ganNhat = dsGiaoDich.Where(g => g.HanhKhachDatTourId == gheId).OrderByDescending(g => g.Id).FirstOrDefault();
            return ganNhat?.TrangThai == "ThatBai" ? ganNhat.NgayXacNhan : null;
        }

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
