using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Bước thu hẹp của RB-37: bỏ LichKhoiHanhTours.SoChoToiDa vì suy ra được
    /// (số vị trí của xe − số dòng ChoKhongMoBans của lịch), và bắt buộc mọi lịch có xe (RB-14).
    /// Chạy SAU Mo14 (đã điền ChoKhongMoBans từ SoChoToiDa).
    /// </summary>
    public partial class Mo15_BoSoChoToiDa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Còn lịch chưa có xe thì dừng hẳn, không đổi gì: cần gán xe cho các lịch đó trước
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoDoChoNgoiId IS NULL)
    THROW 50037, N'Còn lịch khởi hành chưa có xe (SoDoChoNgoiId rỗng). Gán xe cho các lịch đó rồi chạy lại.', 1;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Lich_SoCho",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropColumn(
                name: "SoChoToiDa",
                table: "LichKhoiHanhTours");

            migrationBuilder.AlterColumn<int>(
                name: "SoDoChoNgoiId",
                table: "LichKhoiHanhTours",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Lich_SoCho",
                table: "LichKhoiHanhTours",
                sql: "[SoChoCon] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Lich_SoCho",
                table: "LichKhoiHanhTours");

            migrationBuilder.AlterColumn<int>(
                name: "SoDoChoNgoiId",
                table: "LichKhoiHanhTours",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "SoChoToiDa",
                table: "LichKhoiHanhTours",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Hoàn tác: tính lại số chỗ tối đa từ xe và chỗ không mở bán
            migrationBuilder.Sql(@"
UPDATE l SET SoChoToiDa =
    (SELECT COUNT(*) FROM ViTriChoNgois v WHERE v.SoDoChoNgoiId = l.SoDoChoNgoiId)
  - (SELECT COUNT(*) FROM ChoKhongMoBans k WHERE k.LichKhoiHanhTourId = l.Id)
FROM LichKhoiHanhTours l;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Lich_SoCho",
                table: "LichKhoiHanhTours",
                sql: "[SoChoCon] >= 0 AND [SoChoCon] <= [SoChoToiDa]");
        }
    }
}
