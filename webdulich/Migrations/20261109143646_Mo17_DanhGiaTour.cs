using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo17_DanhGiaTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhGiaTours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LichKhoiHanhTourId = table.Column<int>(type: "int", nullable: false),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgaySua = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DangHien = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaTours", x => x.Id);
                    table.CheckConstraint("CK_DanhGia_SoSao", "[SoSao] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_DanhGiaTours_LichKhoiHanhTours_LichKhoiHanhTourId",
                        column: x => x.LichKhoiHanhTourId,
                        principalTable: "LichKhoiHanhTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DanhGiaTours_TaiKhoans_TaiKhoanId",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaTours_LichKhoiHanhTourId",
                table: "DanhGiaTours",
                column: "LichKhoiHanhTourId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaTours_TaiKhoanId_LichKhoiHanhTourId",
                table: "DanhGiaTours",
                columns: new[] { "TaiKhoanId", "LichKhoiHanhTourId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhGiaTours");
        }
    }
}
