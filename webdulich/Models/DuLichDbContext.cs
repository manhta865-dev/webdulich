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
    .OnDelete(DeleteBehavior.SetNull);

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
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TinNhanLienHe>()
    .HasOne(x => x.TaiKhoan)
    .WithMany()
    .HasForeignKey(x => x.TaiKhoanId)
    .OnDelete(DeleteBehavior.SetNull);

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
                .OnDelete(DeleteBehavior.SetNull);


        }
    }
}