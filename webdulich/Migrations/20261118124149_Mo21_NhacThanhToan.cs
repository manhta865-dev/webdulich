using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo21_NhacThanhToan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhacThanhToans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatTourId = table.Column<int>(type: "int", nullable: false),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    HanDuocNhac = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmailNhan = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayGui = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SoLanThu = table.Column<int>(type: "int", nullable: false),
                    Loi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhacThanhToans", x => x.Id);
                    table.CheckConstraint("CK_Nhac_NgayGui", "([TrangThai] = 'DaGui' AND [NgayGui] IS NOT NULL) OR ([TrangThai] <> 'DaGui' AND [NgayGui] IS NULL)");
                    table.CheckConstraint("CK_Nhac_SoLanThu", "[SoLanThu] BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_Nhac_TrangThai", "[TrangThai] IN ('ChoGui','DaGui','Loi')");
                    table.ForeignKey(
                        name: "FK_NhacThanhToans_DatTours_DatTourId",
                        column: x => x.DatTourId,
                        principalTable: "DatTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NhacThanhToans_TaiKhoans_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NhacThanhToans_DatTourId_TaiKhoanId_HanDuocNhac",
                table: "NhacThanhToans",
                columns: new[] { "DatTourId", "TaiKhoanId", "HanDuocNhac" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhacThanhToans_TaiKhoanId",
                table: "NhacThanhToans",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_NhacThanhToans_TrangThai",
                table: "NhacThanhToans",
                column: "TrangThai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhacThanhToans");
        }
    }
}
