using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo14_ChoKhongMoBan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChoKhongMoBans",
                columns: table => new
                {
                    LichKhoiHanhTourId = table.Column<int>(type: "int", nullable: false),
                    ViTriChoNgoiId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoKhongMoBans", x => new { x.LichKhoiHanhTourId, x.ViTriChoNgoiId });
                    table.ForeignKey(
                        name: "FK_ChoKhongMoBans_LichKhoiHanhTours_LichKhoiHanhTourId",
                        column: x => x.LichKhoiHanhTourId,
                        principalTable: "LichKhoiHanhTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChoKhongMoBans_ViTriChoNgois_ViTriChoNgoiId",
                        column: x => x.ViTriChoNgoiId,
                        principalTable: "ViTriChoNgois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChoKhongMoBans_ViTriChoNgoiId",
                table: "ChoKhongMoBans",
                column: "ViTriChoNgoiId");

            // Chuyển dữ liệu (RB-37): lịch cũ mở bán SoChoToiDa chỗ trên xe có nhiều vị trí hơn
            // thì khóa (số vị trí − SoChoToiDa) chỗ trống ở cuối xe, hàng sau trước. Không khóa chỗ đã có khách.
            // Chỉ điền lịch chưa có dòng nào, nên chạy lại không bị nhân đôi.
            migrationBuilder.Sql(@"
INSERT INTO ChoKhongMoBans (LichKhoiHanhTourId, ViTriChoNgoiId)
SELECT x.LichId, x.ViTriId
FROM (
    SELECT l.Id AS LichId, v.Id AS ViTriId, l.SoChoToiDa,
           ROW_NUMBER() OVER (PARTITION BY l.Id ORDER BY v.Hang DESC, v.Cot DESC) AS ThuTu,
           (SELECT COUNT(*) FROM ViTriChoNgois v2 WHERE v2.SoDoChoNgoiId = l.SoDoChoNgoiId) AS SoViTri
    FROM LichKhoiHanhTours l
    JOIN ViTriChoNgois v ON v.SoDoChoNgoiId = l.SoDoChoNgoiId
    WHERE NOT EXISTS (SELECT 1 FROM ChoNgoiChuyenDis c WHERE c.LichKhoiHanhTourId = l.Id AND c.ViTriChoNgoiId = v.Id)
      AND NOT EXISTS (SELECT 1 FROM ChoKhongMoBans k WHERE k.LichKhoiHanhTourId = l.Id)
) x
WHERE x.ThuTu <= x.SoViTri - x.SoChoToiDa;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChoKhongMoBans");
        }
    }
}
