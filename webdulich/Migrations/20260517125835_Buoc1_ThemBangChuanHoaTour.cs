using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Buoc1_ThemBangChuanHoaTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhuongTienId",
                table: "Tours",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoaiDiaHinhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiDiaHinh = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiDiaHinhs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhongCachs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenPhongCach = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongCachs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhuongTiens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenPhuongTien = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuongTiens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TourLoaiDiaHinhs",
                columns: table => new
                {
                    TourId = table.Column<int>(type: "int", nullable: false),
                    LoaiDiaHinhId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourLoaiDiaHinhs", x => new { x.TourId, x.LoaiDiaHinhId });
                    table.ForeignKey(
                        name: "FK_TourLoaiDiaHinhs_LoaiDiaHinhs_LoaiDiaHinhId",
                        column: x => x.LoaiDiaHinhId,
                        principalTable: "LoaiDiaHinhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourLoaiDiaHinhs_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourPhongCachs",
                columns: table => new
                {
                    TourId = table.Column<int>(type: "int", nullable: false),
                    PhongCachId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourPhongCachs", x => new { x.TourId, x.PhongCachId });
                    table.ForeignKey(
                        name: "FK_TourPhongCachs_PhongCachs_PhongCachId",
                        column: x => x.PhongCachId,
                        principalTable: "PhongCachs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourPhongCachs_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_DanhMucId",
                table: "Tours",
                column: "DanhMucId");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_DiaDiemId",
                table: "Tours",
                column: "DiaDiemId");

            migrationBuilder.CreateIndex(
                name: "IX_Tours_PhuongTienId",
                table: "Tours",
                column: "PhuongTienId");

            migrationBuilder.CreateIndex(
                name: "IX_TourLoaiDiaHinhs_LoaiDiaHinhId",
                table: "TourLoaiDiaHinhs",
                column: "LoaiDiaHinhId");

            migrationBuilder.CreateIndex(
                name: "IX_TourPhongCachs_PhongCachId",
                table: "TourPhongCachs",
                column: "PhongCachId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_DanhMucs_DanhMucId",
                table: "Tours",
                column: "DanhMucId",
                principalTable: "DanhMucs",
                principalColumn: "MaDanhMuc",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_DiaDiems_DiaDiemId",
                table: "Tours",
                column: "DiaDiemId",
                principalTable: "DiaDiems",
                principalColumn: "MaDiaDiem",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_PhuongTiens_PhuongTienId",
                table: "Tours",
                column: "PhuongTienId",
                principalTable: "PhuongTiens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tours_DanhMucs_DanhMucId",
                table: "Tours");

            migrationBuilder.DropForeignKey(
                name: "FK_Tours_DiaDiems_DiaDiemId",
                table: "Tours");

            migrationBuilder.DropForeignKey(
                name: "FK_Tours_PhuongTiens_PhuongTienId",
                table: "Tours");

            migrationBuilder.DropTable(
                name: "PhuongTiens");

            migrationBuilder.DropTable(
                name: "TourLoaiDiaHinhs");

            migrationBuilder.DropTable(
                name: "TourPhongCachs");

            migrationBuilder.DropTable(
                name: "LoaiDiaHinhs");

            migrationBuilder.DropTable(
                name: "PhongCachs");

            migrationBuilder.DropIndex(
                name: "IX_Tours_DanhMucId",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Tours_DiaDiemId",
                table: "Tours");

            migrationBuilder.DropIndex(
                name: "IX_Tours_PhuongTienId",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "PhuongTienId",
                table: "Tours");
        }
    }
}
