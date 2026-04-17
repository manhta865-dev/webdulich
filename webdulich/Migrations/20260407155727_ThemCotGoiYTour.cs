using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class ThemCotGoiYTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoaiDiaHinh",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhongCach",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoNgay",
                table: "Tours",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoaiDiaHinh",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PhongCach",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "SoNgay",
                table: "Tours");
        }
    }
}