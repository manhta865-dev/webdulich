using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Mo28: đồng bộ 4 cột giữa code và CSDL (lệch từ đồ án cơ sở, phát hiện khi đối chiếu ERD).
    /// - DatTours.TenKhach nvarchar(100), DatTours.DienThoai nvarchar(20): CSDL đang cho rỗng, nay bắt buộc
    ///   như code (mọi chỗ tạo, sửa đơn đều bắt nhập).
    /// - Tours.MoTa, Tours.HinhAnh: CSDL đã bắt buộc, chỉ sửa model (trước đây model tưởng cho rỗng).
    ///   Bỏ defaultValue "" EF tự sinh để không tạo ràng buộc DEFAULT thừa.
    /// Down trả về đúng trạng thái CSDL trước Mo28 (DatTours 100/20 cho rỗng; Tours vẫn bắt buộc).
    /// </summary>
    public partial class Mo28_DongBoCotLienHeVaTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MoTa",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HinhAnh",
                table: "Tours",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TenKhach",
                table: "DatTours",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DienThoai",
                table: "DatTours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TenKhach",
                table: "DatTours",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "DienThoai",
                table: "DatTours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
