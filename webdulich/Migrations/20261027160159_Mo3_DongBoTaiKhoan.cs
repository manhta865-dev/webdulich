using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Bước 3a: từ đây code đọc và ghi Email, SoDienThoai. Chạy lại phần tách TaiKhoanDangNhap
    /// cho các tài khoản tạo bằng code cũ sau bước 2 (câu lệnh giống Mo2, chạy lại không sao).
    /// </summary>
    public partial class Mo3_DongBoTaiKhoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE TaiKhoans
SET Email = LTRIM(RTRIM(TaiKhoanDangNhap))
WHERE Email IS NULL AND SoDienThoai IS NULL
  AND LTRIM(RTRIM(TaiKhoanDangNhap)) LIKE '%_@_%._%';

UPDATE TaiKhoans
SET SoDienThoai = LTRIM(RTRIM(TaiKhoanDangNhap))
WHERE Email IS NULL AND SoDienThoai IS NULL
  AND LTRIM(RTRIM(TaiKhoanDangNhap)) LIKE '0[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Chỉ điền dữ liệu, không có gì để gỡ.
        }
    }
}
