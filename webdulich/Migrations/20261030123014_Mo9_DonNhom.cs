using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo9_DonNhom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NguoiTraId",
                table: "GiaoDichThanhToans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DaGiaHan",
                table: "DatTours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToans_NguoiTraId",
                table: "GiaoDichThanhToans",
                column: "NguoiTraId");

            migrationBuilder.AddForeignKey(
                name: "FK_GiaoDichThanhToans_TaiKhoans_NguoiTraId",
                table: "GiaoDichThanhToans",
                column: "NguoiTraId",
                principalTable: "TaiKhoans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GiaoDichThanhToans_TaiKhoans_NguoiTraId",
                table: "GiaoDichThanhToans");

            migrationBuilder.DropIndex(
                name: "IX_GiaoDichThanhToans_NguoiTraId",
                table: "GiaoDichThanhToans");

            migrationBuilder.DropColumn(
                name: "NguoiTraId",
                table: "GiaoDichThanhToans");

            migrationBuilder.DropColumn(
                name: "DaGiaHan",
                table: "DatTours");
        }
    }
}
