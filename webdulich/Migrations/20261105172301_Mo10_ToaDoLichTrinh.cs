using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo10_ToaDoLichTrinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KinhDo",
                table: "LichTrinhTours",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ViDo",
                table: "LichTrinhTours",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LichTrinh_ToaDo",
                table: "LichTrinhTours",
                sql: "([ViDo] IS NULL AND [KinhDo] IS NULL) OR ([ViDo] IS NOT NULL AND [KinhDo] IS NOT NULL AND [ViDo] BETWEEN 8 AND 24 AND [KinhDo] BETWEEN 102 AND 110)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LichTrinh_ToaDo",
                table: "LichTrinhTours");

            migrationBuilder.DropColumn(
                name: "KinhDo",
                table: "LichTrinhTours");

            migrationBuilder.DropColumn(
                name: "ViDo",
                table: "LichTrinhTours");
        }
    }
}
