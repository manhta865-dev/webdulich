using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo11_XuatPhatVaDuongDi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KinhDoXuatPhat",
                table: "Tours",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ViDoXuatPhat",
                table: "Tours",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuongDi",
                table: "LichTrinhTours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tours_XuatPhat",
                table: "Tours",
                sql: "([ViDoXuatPhat] IS NULL AND [KinhDoXuatPhat] IS NULL) OR ([ViDoXuatPhat] IS NOT NULL AND [KinhDoXuatPhat] IS NOT NULL AND [ViDoXuatPhat] BETWEEN 8 AND 24 AND [KinhDoXuatPhat] BETWEEN 102 AND 110)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Tours_XuatPhat",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "KinhDoXuatPhat",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "ViDoXuatPhat",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "DuongDi",
                table: "LichTrinhTours");
        }
    }
}
