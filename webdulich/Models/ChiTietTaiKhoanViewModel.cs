namespace webdulich.Models
{
    // Trang chi tiết tài khoản của admin (Admin/ChiTietTaiKhoan). Chỉ đọc; số liệu tính từ đơn, ghế, giao dịch, đánh giá.
    public class ChiTietTaiKhoanViewModel
    {
        public TaiKhoan TaiKhoan { get; set; } = new();
        public List<DonDatTourDoc> DsDon { get; set; } = new();
        public List<GheNhomCuaTaiKhoan> DsGheNhom { get; set; } = new();
        public List<DanhGiaTour> DsDanhGia { get; set; } = new();
        public decimal TongDaTra { get; set; }

        public int SoDonDaHuy => DsDon.Count(d => d.DaHuy);
        public int SoDonConHieuLuc => DsDon.Count - SoDonDaHuy;
        public int SoGheNhomConGiu => DsGheNhom.Count(g => !g.DaNha);
    }

    // Một ghế tài khoản nhận trong đơn nhóm do người khác làm trưởng nhóm
    public class GheNhomCuaTaiKhoan
    {
        public int DonId { get; set; }
        public string MaChuyenDi { get; set; } = string.Empty;
        public string TenTour { get; set; } = string.Empty;
        public DateTime NgayDi { get; set; }
        public bool DaNha { get; set; }
        public bool DaTra { get; set; }
    }

    // Tên và nhãn vai trò dùng chung ở trang tài khoản admin
    public static class VaiTroHienThi
    {
        public static string Ten(string? vaiTro) => (vaiTro ?? "").Trim().ToLower() switch
        {
            "admin" => "Quản trị viên",
            "huongdanvien" => "Hướng dẫn viên",
            "taixe" => "Tài xế",
            _ => "Khách hàng"
        };

        // Class màu đã có trong admin.css (.ad-role.admin, .staff, .driver, .user)
        public static string Lop(string? vaiTro) => (vaiTro ?? "").Trim().ToLower() switch
        {
            "admin" => "admin",
            "huongdanvien" => "staff",
            "taixe" => "driver",
            _ => "user"
        };
    }
}
