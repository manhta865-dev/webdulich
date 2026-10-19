using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class ThemGiaTheoLichKhoiHanh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "GiaNguoiLon",
                table: "LichKhoiHanhTours",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GiaTreEm",
                table: "LichKhoiHanhTours",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GiaNguoiLon",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropColumn(
                name: "GiaTreEm",
                table: "LichKhoiHanhTours");
        }
    }
}
