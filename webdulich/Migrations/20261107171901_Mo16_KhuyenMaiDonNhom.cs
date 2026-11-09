using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo16_KhuyenMaiDonNhom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TienGiam",
                table: "HanhKhachDatTours",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_HanhKhach_TienGiam",
                table: "HanhKhachDatTours",
                sql: "[TienGiam] >= 0 AND [TienGiam] <= [GiaLucDat]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatTours_GiamDonNhom",
                table: "DatTours",
                sql: "[LoaiDon] = 'CaNhan' OR [SoTienGiam] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_HanhKhach_TienGiam",
                table: "HanhKhachDatTours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatTours_GiamDonNhom",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "TienGiam",
                table: "HanhKhachDatTours");
        }
    }
}
