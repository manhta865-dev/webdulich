using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <summary>
    /// Mo27: xóa hai khóa ngoại trùng trên Tours còn sót từ đồ án cơ sở (tạo tay ngày 02/04/2026, không thuộc migration nào):
    /// FK_Tours_DanhMucs trùng FK_Tours_DanhMucs_DanhMucId, FK_Tours_DiaDiems trùng FK_Tours_DiaDiems_DiaDiemId.
    /// Hai khóa do EF quản lý giữ nguyên nên ràng buộc không đổi. Có kiểm tra tồn tại vì database host có thể không có hai khóa này.
    /// </summary>
    public partial class Mo27_XoaKhoaNgoaiTrungTours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_Tours_DanhMucs]', N'F') IS NOT NULL ALTER TABLE [Tours] DROP CONSTRAINT [FK_Tours_DanhMucs];
IF OBJECT_ID(N'[FK_Tours_DiaDiems]', N'F') IS NOT NULL ALTER TABLE [Tours] DROP CONSTRAINT [FK_Tours_DiaDiems];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Đặt lại đúng như trước (không bắt buộc cho hoạt động, chỉ để hoàn tác được)
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_Tours_DanhMucs]', N'F') IS NULL ALTER TABLE [Tours] ADD CONSTRAINT [FK_Tours_DanhMucs] FOREIGN KEY ([DanhMucId]) REFERENCES [DanhMucs] ([MaDanhMuc]);
IF OBJECT_ID(N'[FK_Tours_DiaDiems]', N'F') IS NULL ALTER TABLE [Tours] ADD CONSTRAINT [FK_Tours_DiaDiems] FOREIGN KEY ([DiaDiemId]) REFERENCES [DiaDiems] ([MaDiaDiem]);");
        }
    }
}
