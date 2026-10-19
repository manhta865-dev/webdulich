using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Buoc5_ThemTaiKhoanIdChoDatTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaiKhoanId",
                table: "DatTours",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatTours_TaiKhoanId",
                table: "DatTours",
                column: "TaiKhoanId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_TaiKhoans_TaiKhoanId",
                table: "DatTours",
                column: "TaiKhoanId",
                principalTable: "TaiKhoans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_TaiKhoans_TaiKhoanId",
                table: "DatTours");

            migrationBuilder.DropIndex(
                name: "IX_DatTours_TaiKhoanId",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "TaiKhoanId",
                table: "DatTours");
        }
    }
}
