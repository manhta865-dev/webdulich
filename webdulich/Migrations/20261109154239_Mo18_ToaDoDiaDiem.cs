using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo18_ToaDoDiaDiem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KinhDo",
                table: "DiaDiems",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ViDo",
                table: "DiaDiems",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DiaDiem_ToaDo",
                table: "DiaDiems",
                sql: "([ViDo] IS NULL AND [KinhDo] IS NULL) OR ([ViDo] IS NOT NULL AND [KinhDo] IS NOT NULL AND [ViDo] BETWEEN -90 AND 90 AND [KinhDo] BETWEEN -180 AND 180)");

            // Điền sẵn vị trí cho địa điểm đã có. So tên không phân biệt hoa thường và dấu (COLLATE ..._CI_AI)
            // nên khớp cả "HÀ GIANG", "Ha Giang". Địa điểm không khớp để rỗng, admin bấm chọn trên bản đồ ở form tour.
            // Đ và D là hai chữ khác nhau với mọi collation, nên ghi cả hai cách viết.
            // USA: tour hiện có dùng ảnh Los Angeles nên đặt ở Los Angeles; admin chọn lại được.
            var dsCoSan = new (string CoDau, string KhongDau, string Vi, string Kinh)[]
            {
                ("Hà Nội", "Ha Noi", "21.028", "105.854"), ("Hạ Long", "Ha Long", "20.951", "107.080"),
                ("Sapa", "Sa Pa", "22.336", "103.844"), ("Hà Giang", "Ha Giang", "22.823", "104.984"),
                ("Ninh Bình", "Ninh Binh", "20.251", "105.975"), ("Quảng Bình", "Quang Binh", "17.468", "106.623"),
                ("Huế", "Hue", "16.463", "107.590"), ("Đà Nẵng", "Da Nang", "16.054", "108.202"),
                ("Hội An", "Hoi An", "15.880", "108.335"), ("Nha Trang", "Nha Trang", "12.238", "109.196"),
                ("Đà Lạt", "Da Lat", "11.940", "108.458"), ("Vũng Tàu", "Vung Tau", "10.346", "107.084"),
                ("Tây Ninh", "Tay Ninh", "11.310", "106.098"), ("Bến Tre", "Ben Tre", "10.241", "106.376"),
                ("An Giang", "An Giang", "10.521", "105.125"), ("Phú Quốc", "Phu Quoc", "10.289", "103.984"),
                ("Cần Thơ", "Can Tho", "10.045", "105.747"), ("Côn Đảo", "Con Dao", "8.683", "106.609"),
                ("USA", "Mỹ", "34.052", "-118.244")
            };
            foreach (var d in dsCoSan)
            {
                migrationBuilder.Sql($"UPDATE DiaDiems SET ViDo = {d.Vi}, KinhDo = {d.Kinh} WHERE ViDo IS NULL " +
                    $"AND LTRIM(RTRIM(TenDiaDiem)) COLLATE Latin1_General_CI_AI IN (N'{d.CoDau}', N'{d.KhongDau}')");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DiaDiem_ToaDo",
                table: "DiaDiems");

            migrationBuilder.DropColumn(
                name: "KinhDo",
                table: "DiaDiems");

            migrationBuilder.DropColumn(
                name: "ViDo",
                table: "DiaDiems");
        }
    }
}
