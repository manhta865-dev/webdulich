using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Buoc2_XoaCotCuTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiaDiem",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "LoaiDiaHinh",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "LoaiTour",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PhongCach",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PhuongTien",
                table: "Tours");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiaDiem",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoaiDiaHinh",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiTour",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhongCach",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhuongTien",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
