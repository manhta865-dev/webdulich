using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Bước 3d: trạng thái lịch khởi hành chỉ lưu quyết định của admin (MoBan, TamNgung, DaHuy).
    /// "Đã hủy" -> DaHuy; "Sắp khởi hành", rỗng hoặc giá trị khác -> MoBan (ngày đi vẫn tính riêng).
    /// </summary>
    public partial class Mo5_TrangThaiLich : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE LichKhoiHanhTours
SET TrangThai = CASE WHEN TrangThai = N'Đã hủy' THEN 'DaHuy' ELSE 'MoBan' END
WHERE TrangThai IS NULL OR TrangThai NOT IN ('MoBan', 'TamNgung', 'DaHuy');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE LichKhoiHanhTours
SET TrangThai = CASE WHEN TrangThai = 'DaHuy' THEN N'Đã hủy' ELSE N'Sắp khởi hành' END
WHERE TrangThai IN ('MoBan', 'TamNgung', 'DaHuy');");
        }
    }
}
