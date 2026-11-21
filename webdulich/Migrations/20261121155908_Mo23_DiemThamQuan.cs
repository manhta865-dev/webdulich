using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo23_DiemThamQuan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiemThamQuans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiaDiemId = table.Column<int>(type: "int", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ViDo = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    KinhDo = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiemThamQuans", x => x.Id);
                    table.CheckConstraint("CK_DiemThamQuan_ToaDo", "[ViDo] BETWEEN 8 AND 24 AND [KinhDo] BETWEEN 102 AND 110");
                    table.ForeignKey(
                        name: "FK_DiemThamQuans_DiaDiems_DiaDiemId",
                        column: x => x.DiaDiemId,
                        principalTable: "DiaDiems",
                        principalColumn: "MaDiaDiem",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichTrinhDiemThamQuans",
                columns: table => new
                {
                    LichTrinhTourId = table.Column<int>(type: "int", nullable: false),
                    DiemThamQuanId = table.Column<int>(type: "int", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichTrinhDiemThamQuans", x => new { x.LichTrinhTourId, x.DiemThamQuanId });
                    table.CheckConstraint("CK_LichTrinhDiem_ThuTu", "[ThuTu] >= 1");
                    table.ForeignKey(
                        name: "FK_LichTrinhDiemThamQuans_DiemThamQuans_DiemThamQuanId",
                        column: x => x.DiemThamQuanId,
                        principalTable: "DiemThamQuans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichTrinhDiemThamQuans_LichTrinhTours_LichTrinhTourId",
                        column: x => x.LichTrinhTourId,
                        principalTable: "LichTrinhTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiemThamQuans_DiaDiemId_Ten",
                table: "DiemThamQuans",
                columns: new[] { "DiaDiemId", "Ten" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichTrinhDiemThamQuans_DiemThamQuanId",
                table: "LichTrinhDiemThamQuans",
                column: "DiemThamQuanId");

            migrationBuilder.CreateIndex(
                name: "IX_LichTrinhDiemThamQuans_LichTrinhTourId_ThuTu",
                table: "LichTrinhDiemThamQuans",
                columns: new[] { "LichTrinhTourId", "ThuTu" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichTrinhDiemThamQuans");

            migrationBuilder.DropTable(
                name: "DiemThamQuans");
        }
    }
}
