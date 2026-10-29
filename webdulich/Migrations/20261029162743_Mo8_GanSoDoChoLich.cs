using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Bước 5b: gán sơ đồ chỗ ngồi cho các lịch đã có, cùng quy tắc với code (GanSoDoChoNgoi):
    /// sơ đồ nhỏ nhất của đúng phương tiện của tour mà đủ số chỗ tối đa. Lịch vượt sức chứa
    /// lớn nhất hoặc tour chưa có phương tiện thì để trống. Chỉ điền lịch chưa có sơ đồ.
    /// </summary>
    public partial class Mo8_GanSoDoChoLich : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE l
SET SoDoChoNgoiId = (
    SELECT TOP 1 s.Id
    FROM SoDoChoNgois s
    WHERE s.PhuongTienId = t.PhuongTienId
      AND (SELECT COUNT(*) FROM ViTriChoNgois v WHERE v.SoDoChoNgoiId = s.Id) >= l.SoChoToiDa
    ORDER BY (SELECT COUNT(*) FROM ViTriChoNgois v WHERE v.SoDoChoNgoiId = s.Id), s.Id)
FROM LichKhoiHanhTours l
JOIN Tours t ON t.Id = l.TourId
WHERE l.SoDoChoNgoiId IS NULL AND t.PhuongTienId IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Chỉ bỏ gán ở lịch chưa có ai chọn chỗ
            migrationBuilder.Sql(@"
UPDATE l SET SoDoChoNgoiId = NULL
FROM LichKhoiHanhTours l
WHERE NOT EXISTS (SELECT 1 FROM ChoNgoiChuyenDis c WHERE c.LichKhoiHanhTourId = l.Id);");
        }
    }
}
