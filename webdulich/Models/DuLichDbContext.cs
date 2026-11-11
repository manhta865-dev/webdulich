using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace webdulich.Models
{
    public class DuLichDbContext : IdentityDbContext<ApplicationUser>
    {
        public DuLichDbContext(DbContextOptions<DuLichDbContext> options)
            : base(options)
        {
        }

        public DbSet<DiaDiem> DiaDiems { get; set; }
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<DatTours> DatTours { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<BaiViet> BaiViets { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<LichKhoiHanhTour> LichKhoiHanhTours { get; set; }
        public DbSet<LichTrinhTour> LichTrinhTours { get; set; }
        public DbSet<TourHinhAnh> TourHinhAnhs { get; set; }
        public DbSet<LoaiDiaHinh> LoaiDiaHinhs { get; set; }
        public DbSet<PhongCach> PhongCachs { get; set; }
        public DbSet<PhuongTien> PhuongTiens { get; set; }
        public DbSet<TourLoaiDiaHinh> TourLoaiDiaHinhs { get; set; }
        public DbSet<TourPhongCach> TourPhongCachs { get; set; }
        public DbSet<HanhKhachDatTour> HanhKhachDatTours { get; set; }
        public DbSet<TinNhanLienHe> TinNhanLienHes { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<PhanCongTour> PhanCongTours { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<GiaoDichThanhToan> GiaoDichThanhToans { get; set; }
        public DbSet<SoDoChoNgoi> SoDoChoNgois { get; set; }
        public DbSet<ViTriChoNgoi> ViTriChoNgois { get; set; }
        public DbSet<ChoNgoiChuyenDi> ChoNgoiChuyenDis { get; set; }
        public DbSet<ChoKhongMoBan> ChoKhongMoBans { get; set; }
        public DbSet<DanhGiaTour> DanhGiaTours { get; set; }
        public DbSet<DonXinNghi> DonXinNghis { get; set; }
        public DbSet<MucLuongNhanVien> MucLuongNhanViens { get; set; }
        public DbSet<KhoanLuong> KhoanLuongs { get; set; }
        public DbSet<BangLuong> BangLuongs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TourLoaiDiaHinh>()
                .HasKey(x => new { x.TourId, x.LoaiDiaHinhId });

            modelBuilder.Entity<TourLoaiDiaHinh>()
                .HasOne(x => x.Tour)
                .WithMany(x => x.TourLoaiDiaHinhs)
                .HasForeignKey(x => x.TourId);

            modelBuilder.Entity<TourLoaiDiaHinh>()
                .HasOne(x => x.LoaiDiaHinh)
                .WithMany(x => x.TourLoaiDiaHinhs)
                .HasForeignKey(x => x.LoaiDiaHinhId);

            modelBuilder.Entity<TourPhongCach>()
                .HasKey(x => new { x.TourId, x.PhongCachId });

            modelBuilder.Entity<TourPhongCach>()
                .HasOne(x => x.Tour)
                .WithMany(x => x.TourPhongCachs)
                .HasForeignKey(x => x.TourId);

            modelBuilder.Entity<TourPhongCach>()
                .HasOne(x => x.PhongCach)
                .WithMany(x => x.TourPhongCachs)
                .HasForeignKey(x => x.PhongCachId);

            modelBuilder.Entity<Tour>()
                .HasOne(x => x.DiaDiemInfo)
                .WithMany()
                .HasForeignKey(x => x.DiaDiemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tour>()
                .HasOne(x => x.DanhMuc)
                .WithMany()
                .HasForeignKey(x => x.DanhMucId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tour>()
                .HasOne(x => x.PhuongTienInfo)
                .WithMany(x => x.Tours)
                .HasForeignKey(x => x.PhuongTienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HanhKhachDatTour>()
    .HasOne(x => x.DatTour)
    .WithMany(x => x.HanhKhachDatTours)
    .HasForeignKey(x => x.DatTourId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DatTours>()
    .HasOne(x => x.TaiKhoan)
    .WithMany(x => x.DatTours)
    .HasForeignKey(x => x.TaiKhoanId)
    .OnDelete(DeleteBehavior.Restrict); // tài khoản đã có đơn thì không xóa được

            modelBuilder.Entity<TourHinhAnh>()
                .HasOne(x => x.Tour)
                .WithMany(x => x.TourHinhAnhs)
                .HasForeignKey(x => x.TourId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LichTrinhTour>()
    .HasOne(x => x.Tour)
    .WithMany(x => x.LichTrinhTours)
    .HasForeignKey(x => x.TourId)
    .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Video>(e =>
            {
                e.Property(x => x.MaYouTube).IsUnicode(false);
                e.HasIndex(x => x.MaYouTube).IsUnique();
                e.HasOne(x => x.Tour).WithMany().HasForeignKey(x => x.TourId).OnDelete(DeleteBehavior.SetNull);
                e.ToTable(t => t.HasCheckConstraint("CK_Video_MaYouTube",
                    "LEN([MaYouTube]) = 11 AND [MaYouTube] NOT LIKE '%[^A-Za-z0-9_-]%'"));
            });
            // Vị trí địa điểm (RB-44): trong nước hay nước ngoài đều được, miễn là tọa độ hợp lệ trên Trái Đất
            modelBuilder.Entity<DiaDiem>(e =>
            {
                e.Property(x => x.ViDo).HasPrecision(9, 6);
                e.Property(x => x.KinhDo).HasPrecision(9, 6);
                e.ToTable(t => t.HasCheckConstraint("CK_DiaDiem_ToaDo",
                    "([ViDo] IS NULL AND [KinhDo] IS NULL) OR ([ViDo] IS NOT NULL AND [KinhDo] IS NOT NULL AND [ViDo] BETWEEN -90 AND 90 AND [KinhDo] BETWEEN -180 AND 180)"));
            });
            // Lương nhân viên (RB-46): tiền decimal(18,2); dữ liệu lương là lịch sử tài chính nên Restrict
            modelBuilder.Entity<MucLuongNhanVien>(e =>
            {
                e.Property(x => x.LuongCoBan).HasPrecision(18, 2);
                e.Property(x => x.TienCongNgay).HasPrecision(18, 2);
                e.HasIndex(x => new { x.NhanVienId, x.ApDungTu }).IsUnique();
                e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.NhanVienId).OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t => t.HasCheckConstraint("CK_MucLuong_SoTien", "[LuongCoBan] >= 0 AND [TienCongNgay] >= 0"));
            });
            modelBuilder.Entity<KhoanLuong>(e =>
            {
                e.Property(x => x.SoTien).HasPrecision(18, 2);
                e.HasIndex(x => new { x.NhanVienId, x.Nam, x.Thang });
                e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.NhanVienId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.NguoiTao).WithMany().HasForeignKey(x => x.NguoiTaoId).OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_KhoanLuong_Thang", "[Thang] BETWEEN 1 AND 12 AND [Nam] BETWEEN 2000 AND 2100");
                    t.HasCheckConstraint("CK_KhoanLuong_SoTien", "[SoTien] <> 0");
                });
            });
            modelBuilder.Entity<BangLuong>(e =>
            {
                e.Property(x => x.LuongCoBan).HasPrecision(18, 2);
                e.Property(x => x.TienCongNgay).HasPrecision(18, 2);
                e.HasIndex(x => new { x.NhanVienId, x.Nam, x.Thang }).IsUnique();
                e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.NhanVienId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.NguoiChot).WithMany().HasForeignKey(x => x.NguoiChotId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.NguoiTra).WithMany().HasForeignKey(x => x.NguoiTraId).OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_BangLuong_Thang", "[Thang] BETWEEN 1 AND 12 AND [Nam] BETWEEN 2000 AND 2100");
                    t.HasCheckConstraint("CK_BangLuong_SoLieu",
                        "[LuongCoBan] >= 0 AND [TienCongNgay] >= 0 AND [SoNgayDiTour] >= 0 AND [SoNgayNghiTruLuong] >= 0 AND [SoNgayCongChuan] > 0");
                    t.HasCheckConstraint("CK_BangLuong_Tra",
                        "([NguoiTraId] IS NULL AND [NgayTra] IS NULL) OR ([NguoiTraId] IS NOT NULL AND [NgayTra] IS NOT NULL)");
                });
            });
            // Đơn xin nghỉ (RB-45): nhân viên đã có đơn thì không xóa được (Restrict), người duyệt cũng vậy
            modelBuilder.Entity<DonXinNghi>(e =>
            {
                e.HasIndex(x => new { x.NhanVienId, x.TuNgay });
                e.HasIndex(x => x.TrangThai);
                e.HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.NhanVienId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.NguoiDuyet).WithMany().HasForeignKey(x => x.NguoiDuyetId).OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_DonNghi_Ngay", "[DenNgay] >= [TuNgay]");
                    t.HasCheckConstraint("CK_DonNghi_TrangThai", "[TrangThai] IN ('ChoDuyet','DaDuyet','TuChoi','DaRut')");
                    t.HasCheckConstraint("CK_DonNghi_Duyet",
                        "([TrangThai] IN ('ChoDuyet','DaRut') AND [NguoiDuyetId] IS NULL AND [NgayDuyet] IS NULL) " +
                        "OR ([TrangThai] IN ('DaDuyet','TuChoi') AND [NguoiDuyetId] IS NOT NULL AND [NgayDuyet] IS NOT NULL)");
                });
            });
            // Đánh giá chuyến đi (RB-43): một tài khoản một đánh giá cho một lịch; giữ đánh giá khi đã có,
            // nên chặn xóa lịch và tài khoản (Restrict) giống dữ liệu đơn đặt
            modelBuilder.Entity<DanhGiaTour>(e =>
            {
                e.HasIndex(x => new { x.TaiKhoanId, x.LichKhoiHanhTourId }).IsUnique();
                e.HasOne(x => x.LichKhoiHanhTour).WithMany().HasForeignKey(x => x.LichKhoiHanhTourId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.TaiKhoan).WithMany().HasForeignKey(x => x.TaiKhoanId).OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t => t.HasCheckConstraint("CK_DanhGia_SoSao", "[SoSao] BETWEEN 1 AND 5"));
            });
            modelBuilder.Entity<BaiViet>(e =>
            {
                e.HasIndex(x => x.DuongDan).IsUnique();
                // Danh sách bài đã đăng, mới nhất trước
                e.HasIndex(x => new { x.TrangThai, x.NgayDang });
                e.HasOne(x => x.TacGia).WithMany().HasForeignKey(x => x.TacGiaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Tour).WithMany().HasForeignKey(x => x.TourId).OnDelete(DeleteBehavior.SetNull);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_BaiViet_TrangThai", "[TrangThai] IN ('Nhap','DaDang')");
                    t.HasCheckConstraint("CK_BaiViet_NgayDang", "[TrangThai] <> 'DaDang' OR [NgayDang] IS NOT NULL");
                });
            });
            modelBuilder.Entity<Tour>(e =>
            {
                e.Property(x => x.ViDoXuatPhat).HasPrecision(9, 6);
                e.Property(x => x.KinhDoXuatPhat).HasPrecision(9, 6);
                e.ToTable(t => t.HasCheckConstraint("CK_Tours_XuatPhat",
                    "([ViDoXuatPhat] IS NULL AND [KinhDoXuatPhat] IS NULL) OR ([ViDoXuatPhat] IS NOT NULL AND [KinhDoXuatPhat] IS NOT NULL AND [ViDoXuatPhat] BETWEEN 8 AND 24 AND [KinhDoXuatPhat] BETWEEN 102 AND 110)"));
            });
            modelBuilder.Entity<LichTrinhTour>(e =>
            {
                e.Property(x => x.ViDo).HasPrecision(9, 6);
                e.Property(x => x.KinhDo).HasPrecision(9, 6);
                e.ToTable(t => t.HasCheckConstraint("CK_LichTrinh_ToaDo",
                    // Phải ghi rõ IS NOT NULL: so sánh với NULL cho kết quả "không xác định" và CHECK sẽ cho qua
                    "([ViDo] IS NULL AND [KinhDo] IS NULL) OR ([ViDo] IS NOT NULL AND [KinhDo] IS NOT NULL AND [ViDo] BETWEEN 8 AND 24 AND [KinhDo] BETWEEN 102 AND 110)"));
            });

            modelBuilder.Entity<LichKhoiHanhTour>()
    .HasOne(x => x.Tour)
    .WithMany(x => x.LichKhoiHanhTours)
    .HasForeignKey(x => x.TourId)
    .OnDelete(DeleteBehavior.Restrict); // tour có lịch thì dùng Ngừng bán, không xóa (RB-13)

            modelBuilder.Entity<TinNhanLienHe>()
    .HasOne(x => x.TaiKhoan)
    .WithMany()
    .HasForeignKey(x => x.TaiKhoanId)
    .OnDelete(DeleteBehavior.Cascade); // tin nhắn thuộc hẳn về tài khoản khách

            modelBuilder.Entity<NhanVien>()
         .HasOne(x => x.TaiKhoan)
    .WithOne()
    .HasForeignKey<NhanVien>(x => x.TaiKhoanId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhanCongTour>()
                .HasOne(x => x.LichKhoiHanhTour)
                .WithMany()
                .HasForeignKey(x => x.LichKhoiHanhTourId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PhanCongTour>()
                .HasOne(x => x.HuongDanVien)
                .WithMany()
                .HasForeignKey(x => x.HuongDanVienId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhanCongTour>()
                .HasOne(x => x.TaiXe)
                .WithMany()
                .HasForeignKey(x => x.TaiXeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PhanCongTour>()
                .HasIndex(x => x.LichKhoiHanhTourId)
                .IsUnique();
            modelBuilder.Entity<KhuyenMai>()
   .HasIndex(x => x.MaKhuyenMai)
   .IsUnique();

            modelBuilder.Entity<DatTours>()
                .HasOne(x => x.KhuyenMai)
                .WithMany(x => x.DatTours)
                .HasForeignKey(x => x.KhuyenMaiId)
                .OnDelete(DeleteBehavior.Restrict); // khuyến mãi đã có đơn dùng thì chỉ tạm ngưng

            modelBuilder.Entity<DatTours>()
                .HasOne(x => x.LichKhoiHanhTour)
                .WithMany()
                .HasForeignKey(x => x.LichKhoiHanhTourId)
                .OnDelete(DeleteBehavior.Restrict); // RB-01

            // ---------- Mô hình dữ liệu sau chuẩn hóa (xem docs/MO_HINH_DU_LIEU.md, mục 6) ----------
            // Ngày đặt: đơn cũ nhận ngày chạy migration, bước chuyển dữ liệu sẽ điền lại nếu cần
            modelBuilder.Entity<DatTours>()
                .Property(x => x.NgayDat)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<DatTours>().Property(x => x.LoaiDon).HasMaxLength(20).HasDefaultValue("CaNhan");
            modelBuilder.Entity<DatTours>().Property(x => x.TrangThai).HasMaxLength(20).HasDefaultValue("GiuCho");
            modelBuilder.Entity<DatTours>().Property(x => x.MaMoi).HasMaxLength(30);

            modelBuilder.Entity<HanhKhachDatTour>().Property(x => x.GiaLucDat).HasPrecision(18, 2);
            modelBuilder.Entity<HanhKhachDatTour>().Property(x => x.TienGiam).HasPrecision(18, 2);
            modelBuilder.Entity<HanhKhachDatTour>().Property(x => x.MaThanhToan).HasMaxLength(30);
            modelBuilder.Entity<HanhKhachDatTour>()
                .HasOne(x => x.TaiKhoan)
                .WithMany()
                .HasForeignKey(x => x.TaiKhoanId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TaiKhoan>().Property(x => x.Email).HasMaxLength(256);
            modelBuilder.Entity<TaiKhoan>().Property(x => x.SoDienThoai).HasMaxLength(20);

            // Mọi lịch phải có xe (RB-14): số chỗ mở bán tính từ xe và chỗ không mở bán (RB-37).
            // Kiểu C# vẫn là int? để code cũ kiểm HasValue không đổi; CSDL là NOT NULL.
            modelBuilder.Entity<LichKhoiHanhTour>()
                .HasOne(x => x.SoDoChoNgoi)
                .WithMany()
                .HasForeignKey(x => x.SoDoChoNgoiId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SoDoChoNgoi>(e =>
            {
                e.Property(x => x.TenSoDo).HasMaxLength(100);
                e.HasOne(x => x.PhuongTien)
                    .WithMany()
                    .HasForeignKey(x => x.PhuongTienId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.ToTable(t => t.HasCheckConstraint("CK_SoDoChoNgoi_KichThuoc", "[SoHang] > 0 AND [SoCot] > 0"));
            });

            modelBuilder.Entity<ViTriChoNgoi>(e =>
            {
                e.Property(x => x.MaViTri).HasMaxLength(10);
                e.HasOne(x => x.SoDoChoNgoi)
                    .WithMany(x => x.ViTriChoNgois)
                    .HasForeignKey(x => x.SoDoChoNgoiId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => new { x.SoDoChoNgoiId, x.MaViTri }).IsUnique();
                e.HasIndex(x => new { x.SoDoChoNgoiId, x.Hang, x.Cot }).IsUnique();
            });

            modelBuilder.Entity<ChoNgoiChuyenDi>(e =>
            {
                e.HasKey(x => new { x.LichKhoiHanhTourId, x.ViTriChoNgoiId });
                e.HasOne(x => x.LichKhoiHanhTour)
                    .WithMany()
                    .HasForeignKey(x => x.LichKhoiHanhTourId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ViTriChoNgoi)
                    .WithMany()
                    .HasForeignKey(x => x.ViTriChoNgoiId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.HanhKhachDatTour)
                    .WithMany()
                    .HasForeignKey(x => x.HanhKhachDatTourId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.HanhKhachDatTourId).IsUnique();
            });

            // Chỗ không mở bán của từng lịch (RB-37). Xóa lịch thì các dòng này đi theo (dữ liệu con thuần túy);
            // vị trí của sơ đồ đang được khóa thì không xóa được (Restrict).
            modelBuilder.Entity<ChoKhongMoBan>(e =>
            {
                e.HasKey(x => new { x.LichKhoiHanhTourId, x.ViTriChoNgoiId });
                e.HasOne(x => x.LichKhoiHanhTour)
                    .WithMany()
                    .HasForeignKey(x => x.LichKhoiHanhTourId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.ViTriChoNgoi)
                    .WithMany()
                    .HasForeignKey(x => x.ViTriChoNgoiId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GiaoDichThanhToan>(e =>
            {
                e.Property(x => x.SoTien).HasPrecision(18, 2);
                e.Property(x => x.PhuongThuc).HasMaxLength(50);
                e.Property(x => x.TrangThai).HasMaxLength(20);
                e.Property(x => x.MaThamChieuNganHang).HasMaxLength(100);
                e.HasOne(x => x.DatTour)
                    .WithMany()
                    .HasForeignKey(x => x.DatTourId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.HanhKhachDatTour)
                    .WithMany()
                    .HasForeignKey(x => x.HanhKhachDatTourId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.NguoiXacNhan)
                    .WithMany()
                    .HasForeignKey(x => x.NguoiXacNhanId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.NguoiTra)
                    .WithMany()
                    .HasForeignKey(x => x.NguoiTraId)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => x.MaThamChieuNganHang).IsUnique();
                // Mỗi đơn hoặc ghế tối đa một giao dịch thành công
                e.HasIndex(x => x.DatTourId).IsUnique().HasFilter("[TrangThai] = 'ThanhCong' AND [DatTourId] IS NOT NULL");
                e.HasIndex(x => x.HanhKhachDatTourId).IsUnique().HasFilter("[TrangThai] = 'ThanhCong' AND [HanhKhachDatTourId] IS NOT NULL");
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_GiaoDich_MotDoiTuong",
                        "([DatTourId] IS NOT NULL AND [HanhKhachDatTourId] IS NULL) OR ([DatTourId] IS NULL AND [HanhKhachDatTourId] IS NOT NULL)");
                    t.HasCheckConstraint("CK_GiaoDich_TrangThai", "[TrangThai] IN ('ChoXacNhan','ThanhCong','ThatBai')");
                    t.HasCheckConstraint("CK_GiaoDich_SoTien", "[SoTien] > 0");
                });
            });

            // ---------- Bước thu hẹp: khóa duy nhất và CHECK (RB-02, 03, 06, 10, 11) ----------
            modelBuilder.Entity<DatTours>(e =>
            {
                e.Property(x => x.MaChuyenDi).HasMaxLength(30);
                e.HasIndex(x => x.MaChuyenDi).IsUnique();
                e.HasIndex(x => x.MaMoi).IsUnique(); // EF tự lọc IS NOT NULL
                e.Property(x => x.SoTienGiam).HasPrecision(18, 2);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_DatTours_LoaiDon", "[LoaiDon] IN ('CaNhan','Nhom')");
                    t.HasCheckConstraint("CK_DatTours_TrangThai", "[TrangThai] IN ('GiuCho','DaXacNhan','DaHuy')");
                    t.HasCheckConstraint("CK_DatTours_SoTienGiam", "[SoTienGiam] >= 0");
                    // RB-40: đơn nhóm lưu tiền giảm theo từng ghế, không lưu ở đơn, để không tính giảm hai lần
                    t.HasCheckConstraint("CK_DatTours_GiamDonNhom", "[LoaiDon] = 'CaNhan' OR [SoTienGiam] = 0");
                });
            });

            modelBuilder.Entity<HanhKhachDatTour>(e =>
            {
                // Một tài khoản chỉ nhận tối đa một ghế trong cùng một đơn
                e.HasIndex(x => new { x.DatTourId, x.TaiKhoanId }).IsUnique().HasFilter("[TaiKhoanId] IS NOT NULL");
                e.HasIndex(x => x.DatTourId, "IX_HanhKhachDatTours_DatTourId"); // giữ chỉ mục cho khóa ngoại
                e.HasIndex(x => x.MaThanhToan).IsUnique();
                e.Property(x => x.LoaiHanhKhach).HasMaxLength(20);
                e.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_HanhKhach_Loai", "[LoaiHanhKhach] IN (N'Người lớn', N'Trẻ nhỏ')");
                    t.HasCheckConstraint("CK_HanhKhach_GiaLucDat", "[GiaLucDat] >= 0");
                    // RB-40: phần giảm của ghế không âm, không vượt giá ghế
                    t.HasCheckConstraint("CK_HanhKhach_TienGiam", "[TienGiam] >= 0 AND [TienGiam] <= [GiaLucDat]");
                });
            });

            modelBuilder.Entity<TaiKhoan>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique();
                e.HasIndex(x => x.SoDienThoai).IsUnique();
                e.ToTable(t => t.HasCheckConstraint("CK_TaiKhoan_DinhDanh", "[Email] IS NOT NULL OR [SoDienThoai] IS NOT NULL"));
            });

            modelBuilder.Entity<LichKhoiHanhTour>(e =>
            {
                e.Property(x => x.TrangThai).HasMaxLength(20);
                e.ToTable(t =>
                {
                    // Cận trên (SoChoCon <= số chỗ mở bán) liên quan bảng khác nên kiểm trong code (SucChuaLich.TinhLaiSoChoCon) và doi_chieu.sql
                    t.HasCheckConstraint("CK_Lich_SoCho", "[SoChoCon] >= 0");
                    t.HasCheckConstraint("CK_Lich_TrangThai", "[TrangThai] IN ('MoBan','TamNgung','DaHuy')");
                });
            });

            modelBuilder.Entity<KhuyenMai>(e =>
            {
                e.Property(x => x.TrangThai).HasMaxLength(20);
                e.ToTable(t => t.HasCheckConstraint("CK_KhuyenMai_TrangThai", "[TrangThai] IN ('HoatDong','TamNgung')"));
            });


        }
    }
}