using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangKhuyenMai : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KhuyenMaiId",
                table: "DatTours",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaKhuyenMai",
                table: "DatTours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienGiam",
                table: "DatTours",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TongTienTruocGiam",
                table: "DatTours",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "KhuyenMais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKhuyenMai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaKhuyenMai = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KieuGiamGia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GiaTriGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MauNen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoLuotDungToiDa = table.Column<int>(type: "int", nullable: true),
                    SoLuotDaDung = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuyenMais", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatTours_KhuyenMaiId",
                table: "DatTours",
                column: "KhuyenMaiId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuyenMais_MaKhuyenMai",
                table: "KhuyenMais",
                column: "MaKhuyenMai",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_KhuyenMais_KhuyenMaiId",
                table: "DatTours",
                column: "KhuyenMaiId",
                principalTable: "KhuyenMais",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_KhuyenMais_KhuyenMaiId",
                table: "DatTours");

            migrationBuilder.DropTable(
                name: "KhuyenMais");

            migrationBuilder.DropIndex(
                name: "IX_DatTours_KhuyenMaiId",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "KhuyenMaiId",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "MaKhuyenMai",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "SoTienGiam",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "TongTienTruocGiam",
                table: "DatTours");
        }
    }
}
