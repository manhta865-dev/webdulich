using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo12_BaiViet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaiViets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    TomTat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnhBia = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDang = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TacGiaId = table.Column<int>(type: "int", nullable: false),
                    TourId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiViets", x => x.Id);
                    table.CheckConstraint("CK_BaiViet_NgayDang", "[TrangThai] <> 'DaDang' OR [NgayDang] IS NOT NULL");
                    table.CheckConstraint("CK_BaiViet_TrangThai", "[TrangThai] IN ('Nhap','DaDang')");
                    table.ForeignKey(
                        name: "FK_BaiViets_TaiKhoans_TacGiaId",
                        column: x => x.TacGiaId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaiViets_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaiViets_DuongDan",
                table: "BaiViets",
                column: "DuongDan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaiViets_TacGiaId",
                table: "BaiViets",
                column: "TacGiaId");

            migrationBuilder.CreateIndex(
                name: "IX_BaiViets_TourId",
                table: "BaiViets",
                column: "TourId");

            migrationBuilder.CreateIndex(
                name: "IX_BaiViets_TrangThai_NgayDang",
                table: "BaiViets",
                columns: new[] { "TrangThai", "NgayDang" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaiViets");
        }
    }
}
