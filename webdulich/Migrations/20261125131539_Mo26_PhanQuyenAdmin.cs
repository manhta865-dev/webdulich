using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo26_PhanQuyenAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaiKhoanQuyens",
                columns: table => new
                {
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    MaQuyen = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanQuyens", x => new { x.TaiKhoanId, x.MaQuyen });
                    table.CheckConstraint("CK_TaiKhoanQuyen_MaQuyen", "[MaQuyen] IN ('TourLich','DonThanhToan','DoanhThuTienVao','NoiDung','KhachNhanSu','TaiKhoanQuyen')");
                    table.ForeignKey(
                        name: "FK_TaiKhoanQuyens_TaiKhoans_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Mọi tài khoản đang là admin nhận đủ 6 nhóm quyền, để sau khi cập nhật không ai mất quyền đang có (RB-51)
            migrationBuilder.Sql(@"
INSERT INTO [TaiKhoanQuyens] ([TaiKhoanId], [MaQuyen])
SELECT t.[Id], q.[Ma]
FROM [TaiKhoans] t
CROSS JOIN (VALUES ('TourLich'), ('DonThanhToan'), ('DoanhThuTienVao'), ('NoiDung'), ('KhachNhanSu'), ('TaiKhoanQuyen')) q([Ma])
WHERE t.[VaiTro] = 'admin'
  AND NOT EXISTS (SELECT 1 FROM [TaiKhoanQuyens] x WHERE x.[TaiKhoanId] = t.[Id] AND x.[MaQuyen] = q.[Ma]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaiKhoanQuyens");
        }
    }
}
