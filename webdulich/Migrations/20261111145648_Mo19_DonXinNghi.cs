using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo19_DonXinNghi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonXinNghis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienId = table.Column<int>(type: "int", nullable: false),
                    TuNgay = table.Column<DateTime>(type: "date", nullable: false),
                    DenNgay = table.Column<DateTime>(type: "date", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgayGui = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiDuyetId = table.Column<int>(type: "int", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChuDuyet = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonXinNghis", x => x.Id);
                    table.CheckConstraint("CK_DonNghi_Duyet", "([TrangThai] IN ('ChoDuyet','DaRut') AND [NguoiDuyetId] IS NULL AND [NgayDuyet] IS NULL) OR ([TrangThai] IN ('DaDuyet','TuChoi') AND [NguoiDuyetId] IS NOT NULL AND [NgayDuyet] IS NOT NULL)");
                    table.CheckConstraint("CK_DonNghi_Ngay", "[DenNgay] >= [TuNgay]");
                    table.CheckConstraint("CK_DonNghi_TrangThai", "[TrangThai] IN ('ChoDuyet','DaDuyet','TuChoi','DaRut')");
                    table.ForeignKey(
                        name: "FK_DonXinNghis_NhanViens_NhanVienId",
                        column: x => x.NhanVienId,
                        principalTable: "NhanViens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonXinNghis_TaiKhoans_NguoiDuyetId",
                        column: x => x.NguoiDuyetId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonXinNghis_NguoiDuyetId",
                table: "DonXinNghis",
                column: "NguoiDuyetId");

            migrationBuilder.CreateIndex(
                name: "IX_DonXinNghis_NhanVienId_TuNgay",
                table: "DonXinNghis",
                columns: new[] { "NhanVienId", "TuNgay" });

            migrationBuilder.CreateIndex(
                name: "IX_DonXinNghis_TrangThai",
                table: "DonXinNghis",
                column: "TrangThai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonXinNghis");
        }
    }
}
