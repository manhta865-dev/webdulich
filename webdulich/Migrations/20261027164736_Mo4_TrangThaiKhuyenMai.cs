using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Bước 3c: đổi giá trị trạng thái khuyến mãi sang bộ mới (cùng lúc với code).
    /// DangHienThi -> HoatDong, TamAn -> TamNgung. Lượt đã dùng từ nay đếm từ đơn chưa hủy.
    /// </summary>
    public partial class Mo4_TrangThaiKhuyenMai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE KhuyenMais
SET TrangThai = CASE WHEN TrangThai = 'TamAn' THEN 'TamNgung' ELSE 'HoatDong' END
WHERE TrangThai NOT IN ('HoatDong', 'TamNgung');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE KhuyenMais
SET TrangThai = CASE WHEN TrangThai = 'TamNgung' THEN 'TamAn' ELSE 'DangHienThi' END
WHERE TrangThai IN ('HoatDong', 'TamNgung');");
        }
    }
}
