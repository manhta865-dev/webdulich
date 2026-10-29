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
            modelBuilder.Entity<HanhKhachDatTour>().Property(x => x.MaThanhToan).HasMaxLength(30);
            modelBuilder.Entity<HanhKhachDatTour>()
                .HasOne(x => x.TaiKhoan)
                .WithMany()
                .HasForeignKey(x => x.TaiKhoanId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TaiKhoan>().Property(x => x.Email).HasMaxLength(256);
            modelBuilder.Entity<TaiKhoan>().Property(x => x.SoDienThoai).HasMaxLength(20);

            modelBuilder.Entity<LichKhoiHanhTour>()
                .HasOne(x => x.SoDoChoNgoi)
                .WithMany()
                .HasForeignKey(x => x.SoDoChoNgoiId)
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
                    t.HasCheckConstraint("CK_Lich_SoCho", "[SoChoCon] >= 0 AND [SoChoCon] <= [SoChoToiDa]");
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