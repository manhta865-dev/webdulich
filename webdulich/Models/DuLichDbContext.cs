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
    }
}