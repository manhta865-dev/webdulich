using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo20_LuongNhanVien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BangLuongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienId = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    Thang = table.Column<int>(type: "int", nullable: false),
                    LuongCoBan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TienCongNgay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SoNgayDiTour = table.Column<int>(type: "int", nullable: false),
                    SoNgayNghiTruLuong = table.Column<int>(type: "int", nullable: false),
                    SoNgayCongChuan = table.Column<int>(type: "int", nullable: false),
                    NguoiChotId = table.Column<int>(type: "int", nullable: false),
                    NgayChot = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTraId = table.Column<int>(type: "int", nullable: true),
                    NgayTra = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BangLuongs", x => x.Id);
                    table.CheckConstraint("CK_BangLuong_SoLieu", "[LuongCoBan] >= 0 AND [TienCongNgay] >= 0 AND [SoNgayDiTour] >= 0 AND [SoNgayNghiTruLuong] >= 0 AND [SoNgayCongChuan] > 0");
                    table.CheckConstraint("CK_BangLuong_Thang", "[Thang] BETWEEN 1 AND 12 AND [Nam] BETWEEN 2000 AND 2100");
                    table.CheckConstraint("CK_BangLuong_Tra", "([NguoiTraId] IS NULL AND [NgayTra] IS NULL) OR ([NguoiTraId] IS NOT NULL AND [NgayTra] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_BangLuongs_NhanViens_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BangLuongs_TaiKhoans_NguoiChotId",
                        column: x => x.NguoiChotId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BangLuongs_TaiKhoans_NguoiTraId",
                        column: x => x.NguoiTraId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KhoanLuongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienId = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    Thang = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NguoiTaoId = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhoanLuongs", x => x.Id);
                    table.CheckConstraint("CK_KhoanLuong_SoTien", "[SoTien] <> 0");
                    table.CheckConstraint("CK_KhoanLuong_Thang", "[Thang] BETWEEN 1 AND 12 AND [Nam] BETWEEN 2000 AND 2100");
                    table.ForeignKey(
                        name: "FK_KhoanLuongs_NhanViens_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KhoanLuongs_TaiKhoans_NguoiTaoId",
                        column: x => x.NguoiTaoId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MucLuongNhanViens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienId = table.Column<int>(type: "int", nullable: false),
                    LuongCoBan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TienCongNgay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApDungTu = table.Column<DateTime>(type: "date", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucLuongNhanViens", x => x.Id);
                    table.CheckConstraint("CK_MucLuong_SoTien", "[LuongCoBan] >= 0 AND [TienCongNgay] >= 0");
                    table.ForeignKey(
                        name: "FK_MucLuongNhanViens_NhanViens_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BangLuongs_NguoiChotId",
                table: "BangLuongs",
                column: "NguoiChotId");

            migrationBuilder.CreateIndex(
                name: "IX_BangLuongs_NguoiTraId",
                table: "BangLuongs",
                column: "NguoiTraId");

            migrationBuilder.CreateIndex(
                name: "IX_BangLuongs_NhanVienId_Nam_Thang",
                table: "BangLuongs",
                columns: new[] { "NhanVienId", "Nam", "Thang" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KhoanLuongs_NguoiTaoId",
                table: "KhoanLuongs",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_KhoanLuongs_NhanVienId_Nam_Thang",
                table: "KhoanLuongs",
                columns: new[] { "NhanVienId", "Nam", "Thang" });

            migrationBuilder.CreateIndex(
                name: "IX_MucLuongNhanViens_NhanVienId_ApDungTu",
                table: "MucLuongNhanViens",
                columns: new[] { "NhanVienId", "ApDungTu" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BangLuongs");

            migrationBuilder.DropTable(
                name: "KhoanLuongs");

            migrationBuilder.DropTable(
                name: "MucLuongNhanViens");
        }
    }
}
