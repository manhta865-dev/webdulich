using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo22_GiaoDichNganHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiaoDichNganHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nguon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaGiaoDichNguon = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NganHang = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SoTaiKhoan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ThoiGianGiaoDich = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MaThamChieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayNhan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KetQua = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GiaoDichThanhToanId = table.Column<int>(type: "int", nullable: true),
                    NguoiXuLyId = table.Column<int>(type: "int", nullable: true),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChuXuLy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichNganHangs", x => x.Id);
                    table.CheckConstraint("CK_TienVao_KetQua", "[KetQua] IN ('DaKhop','KhongTimThayMa','SaiSoTien','DaTraRoi','GheDaNha','DonDaHuy')");
                    table.CheckConstraint("CK_TienVao_Khop", "([KetQua] = 'DaKhop' AND [GiaoDichThanhToanId] IS NOT NULL) OR ([KetQua] <> 'DaKhop' AND [GiaoDichThanhToanId] IS NULL)");
                    table.CheckConstraint("CK_TienVao_Nguon", "[Nguon] IN ('SePay','GiaLap')");
                    table.CheckConstraint("CK_TienVao_SoTien", "[SoTien] > 0");
                    table.CheckConstraint("CK_TienVao_XuLy", "([NguoiXuLyId] IS NULL AND [NgayXuLy] IS NULL) OR ([NguoiXuLyId] IS NOT NULL AND [NgayXuLy] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_GiaoDichNganHangs_GiaoDichThanhToans_GiaoDichThanhToanId",
                        column: x => x.GiaoDichThanhToanId,
                        principalTable: "GiaoDichThanhToans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichNganHangs_TaiKhoans_NguoiXuLyId",
                        column: x => x.NguoiXuLyId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichNganHangs_GiaoDichThanhToanId",
                table: "GiaoDichNganHangs",
                column: "GiaoDichThanhToanId",
                unique: true,
                filter: "[GiaoDichThanhToanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichNganHangs_KetQua_NgayNhan",
                table: "GiaoDichNganHangs",
                columns: new[] { "KetQua", "NgayNhan" });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichNganHangs_NguoiXuLyId",
                table: "GiaoDichNganHangs",
                column: "NguoiXuLyId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichNganHangs_Nguon_MaGiaoDichNguon",
                table: "GiaoDichNganHangs",
                columns: new[] { "Nguon", "MaGiaoDichNguon" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiaoDichNganHangs");
        }
    }
}
