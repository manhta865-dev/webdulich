namespace webdulich.Models
{
    // Một nhóm quyền của một tài khoản quản trị (Mo26, RB-51). Bảng nối toàn khóa: PK (TaiKhoanId, MaQuyen).
    public class TaiKhoanQuyen
    {
        public int TaiKhoanId { get; set; }
        public string MaQuyen { get; set; } = string.Empty;

        public TaiKhoan? TaiKhoan { get; set; }
    }

    // Danh mục nhóm quyền cố định (mã cố định trong code, CHECK ở CSDL) và cách tra trang nào thuộc nhóm nào.
    // Trang Tổng quan: mọi tài khoản quản trị đều xem được. Nhóm "Tài khoản và phân quyền" là quyền của admin chính.
    public static class QuyenAdmin
    {
        public const string TourLich = "TourLich";
        public const string DonThanhToan = "DonThanhToan";
        public const string DoanhThuTienVao = "DoanhThuTienVao";
        public const string NoiDung = "NoiDung";
        public const string KhachNhanSu = "KhachNhanSu";
        public const string TaiKhoanQuyen = "TaiKhoanQuyen";

        public static readonly (string Ma, string Ten, string MoTa)[] TatCa =
        {
            (TourLich, "Tour và lịch", "Quản lý tour, lịch đi, lịch trình, điểm tham quan, phân công"),
            (DonThanhToan, "Đơn và thanh toán", "Đơn đặt tour, xác nhận tiền, đặt tại quầy, khuyến mãi"),
            (DoanhThuTienVao, "Doanh thu và tiền vào", "Báo cáo doanh thu, xuất báo cáo, giao dịch ngân hàng"),
            (NoiDung, "Nội dung", "Bài viết, video, đánh giá tour"),
            (KhachNhanSu, "Khách và nhân sự", "Tin nhắn, nhân sự, đơn xin nghỉ, lương"),
            (TaiKhoanQuyen, "Tài khoản và phân quyền", "Sửa, xóa tài khoản và cấp quyền cho quản trị viên (admin chính)")
        };

        public static bool HopLe(string ma) => TatCa.Any(q => q.Ma == ma);

        // Các action của AdminController theo nhóm. Action không có trong danh sách (trừ Tổng quan) cần quyền admin chính,
        // để trang mới thêm sau này mặc định bị khóa chứ không mặc định mở.
        private static readonly Dictionary<string, string> ActionAdmin = TaoBang(new Dictionary<string, string[]>
        {
            [TourLich] = new[]
            {
                "Index", "Create", "Edit", "Delete", "DoiNgungBan", "XoaHinhTour",
                "QuanLyLichKhoiHanh", "LichKhoiHanh", "ThemLichKhoiHanh", "CapNhatLichKhoiHanh", "HuyLichKhoiHanh",
                "DoiTrangThaiBanLich", "ChoMoBan", "CapNhatDiemXuatPhat",
                "QuanLyLichTrinh", "LichTrinh", "ThemLichTrinh", "XoaLichTrinh", "CapNhatToaDoLichTrinh",
                "PhanCongTour", "ThemPhanCongTour", "SuaPhanCongTour", "XoaPhanCongTour"
            },
            [DonThanhToan] = new[]
            {
                "DonDatTour", "DonDatTourTheoTour", "SuaDonDatTour", "HuyDonDatTour", "DoiSoNguoiDon",
                "XacNhanDaNhanTien", "XacNhanGiaoDichGhe",
                "KhuyenMai", "ThemKhuyenMai", "SuaKhuyenMai", "XoaKhuyenMai", "DoiTrangThaiKhuyenMai"
            },
            [DoanhThuTienVao] = new[] { "DoanhThu", "BaoCaoDoanhThuPdf", "XuatDoanhThuExcel" },
            [KhachNhanSu] = new[]
            {
                "TinNhan", "ChiTietTinNhan", "LayChiTietTinNhan", "TraLoiTinNhan", "TraLoiTinNhanAjax", "XoaCuocTroChuyen",
                "NhanSu", "ThemNhanVien", "DoiTrangThaiNhanVien"
            },
            [TaiKhoanQuyen] = new[] { "TaiKhoan", "ChiTietTaiKhoan", "SuaTaiKhoan", "XoaTaiKhoan" }
        });

        // Các controller Admin* khác: cả controller thuộc một nhóm
        private static readonly Dictionary<string, string> ControllerAdmin = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AdminDiemThamQuan"] = TourLich,
            ["AdminDatTaiQuay"] = DonThanhToan,
            ["AdminTienVao"] = DoanhThuTienVao,
            ["AdminBaiViet"] = NoiDung,
            ["AdminVideo"] = NoiDung,
            ["AdminDanhGia"] = NoiDung,
            ["AdminDonXinNghi"] = KhachNhanSu,
            ["AdminLuong"] = KhachNhanSu
        };

        private static Dictionary<string, string> TaoBang(Dictionary<string, string[]> theoNhom)
        {
            var bang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (nhom, dsAction) in theoNhom)
            {
                foreach (var a in dsAction)
                {
                    bang[a] = nhom;
                }
            }
            return bang;
        }

        // Nhóm quyền cần có để vào một trang quản trị; null là trang không thuộc khu quản trị hoặc ai là admin cũng vào được
        public static string? CanCo(string? controller, string? action)
        {
            if (string.IsNullOrEmpty(controller) || !controller.StartsWith("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            if (string.Equals(controller, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(action, "TongQuan", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                return action != null && ActionAdmin.TryGetValue(action, out var nhom) ? nhom : TaiKhoanQuyen;
            }
            return ControllerAdmin.TryGetValue(controller, out var nhomController) ? nhomController : TaiKhoanQuyen;
        }

        private const string KhoaBoNho = "QuyenAdmin.CuaToi";

        // Quyền của tài khoản đang đăng nhập; đọc CSDL một lần mỗi yêu cầu (đổi quyền có hiệu lực ngay, không cần đăng nhập lại)
        public static IReadOnlySet<string> CuaToi(HttpContext http, DuLichDbContext db)
        {
            if (http.Items.TryGetValue(KhoaBoNho, out var daCo) && daCo is IReadOnlySet<string> ds)
            {
                return ds;
            }
            var id = http.Session.GetInt32("taiKhoanId");
            IReadOnlySet<string> ketQua = id.HasValue && http.Session.GetString("role") == "admin"
                ? db.TaiKhoanQuyens.Where(q => q.TaiKhoanId == id.Value).Select(q => q.MaQuyen).ToHashSet()
                : new HashSet<string>();
            http.Items[KhoaBoNho] = ketQua;
            return ketQua;
        }

        public static bool Co(HttpContext http, DuLichDbContext db, string ma) => CuaToi(http, db).Contains(ma);
    }
}
