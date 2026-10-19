using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class ThemNhanVienVaPhanCongTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhanViens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiNhanVien = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DangLamViec = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanViens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhanViens_TaiKhoans_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhanCongTours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LichKhoiHanhTourId = table.Column<int>(type: "int", nullable: false),
                    HuongDanVienId = table.Column<int>(type: "int", nullable: false),
                    TaiXeId = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayPhanCong = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanCongTours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhanCongTours_LichKhoiHanhTours_LichKhoiHanhTourId",
                        column: x => x.LichKhoiHanhTourId,
                        principalTable: "LichKhoiHanhTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhanCongTours_NhanViens_HuongDanVienId",
                        column: x => x.HuongDanVienId,
                        principalTable: "NhanViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhanCongTours_NhanViens_TaiXeId",
                        column: x => x.TaiXeId,
                        principalTable: "NhanViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NhanViens_TaiKhoanId",
                table: "NhanViens",
                column: "TaiKhoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongTours_HuongDanVienId",
                table: "PhanCongTours",
                column: "HuongDanVienId");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongTours_LichKhoiHanhTourId",
                table: "PhanCongTours",
                column: "LichKhoiHanhTourId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongTours_TaiXeId",
                table: "PhanCongTours",
                column: "TaiXeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhanCongTours");

            migrationBuilder.DropTable(
                name: "NhanViens");
        }
    }
}
