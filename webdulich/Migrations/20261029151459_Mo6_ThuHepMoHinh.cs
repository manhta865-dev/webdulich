using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo6_ThuHepMoHinh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chốt chặn: dữ liệu phải đạt các điều kiện dưới đây thì mới thu hẹp. Nếu không đạt, dừng cả
            // migration (không đổi gì) và báo lý do; xử lý dữ liệu theo docs/sql/doi_chieu.sql rồi chạy lại.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM DatTours WHERE LichKhoiHanhTourId IS NULL)
    THROW 50001, N'Còn đơn chưa gắn lịch khởi hành (truy vấn C trong doi_chieu.sql).', 1;
IF EXISTS (SELECT MaChuyenDi FROM DatTours GROUP BY MaChuyenDi HAVING COUNT(*) > 1)
    THROW 50002, N'Có mã chuyến đi bị trùng.', 1;
IF EXISTS (SELECT 1 FROM DatTours WHERE LEN(MaChuyenDi) > 30)
    THROW 50003, N'Có mã chuyến đi dài hơn 30 ký tự.', 1;
IF EXISTS (SELECT 1 FROM TinNhanLienHes WHERE TaiKhoanId IS NULL)
    THROW 50004, N'Còn tin nhắn không gắn tài khoản.', 1;
IF EXISTS (SELECT 1 FROM TaiKhoans WHERE Email IS NULL AND SoDienThoai IS NULL)
    THROW 50005, N'Còn tài khoản chưa có email lẫn số điện thoại (truy vấn A).', 1;
IF EXISTS (SELECT Email FROM TaiKhoans WHERE Email IS NOT NULL GROUP BY Email HAVING COUNT(*) > 1)
    THROW 50006, N'Có email trùng giữa các tài khoản.', 1;
IF EXISTS (SELECT SoDienThoai FROM TaiKhoans WHERE SoDienThoai IS NOT NULL GROUP BY SoDienThoai HAVING COUNT(*) > 1)
    THROW 50007, N'Có số điện thoại trùng giữa các tài khoản.', 1;
IF EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE SoChoCon < 0 OR SoChoCon > SoChoToiDa)
    THROW 50008, N'Có lịch có số chỗ còn ngoài khoảng 0 đến số chỗ tối đa (truy vấn F).', 1;
IF EXISTS (SELECT 1 FROM LichKhoiHanhTours WHERE TrangThai NOT IN ('MoBan','TamNgung','DaHuy'))
    THROW 50009, N'Có lịch mang trạng thái cũ (chạy migration Mo5 trước).', 1;
IF EXISTS (SELECT 1 FROM KhuyenMais WHERE TrangThai NOT IN ('HoatDong','TamNgung'))
    THROW 50010, N'Có khuyến mãi mang trạng thái cũ (chạy migration Mo4 trước).', 1;
IF EXISTS (SELECT 1 FROM HanhKhachDatTours WHERE LoaiHanhKhach NOT IN (N'Người lớn', N'Trẻ nhỏ'))
    THROW 50011, N'Có hành khách mang loại khác Người lớn / Trẻ nhỏ.', 1;
IF EXISTS (SELECT 1 FROM DatTours WHERE TrangThai NOT IN ('GiuCho','DaXacNhan','DaHuy') OR LoaiDon NOT IN ('CaNhan','Nhom'))
    THROW 50012, N'Có đơn mang trạng thái hoặc loại đơn không hợp lệ.', 1;");

            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_KhuyenMais_KhuyenMaiId",
                table: "DatTours");

            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_LichKhoiHanhTours_LichKhoiHanhTourId",
                table: "DatTours");

            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_TaiKhoans_TaiKhoanId",
                table: "DatTours");

            // DB thật có thể đặt tên khóa ngoại khác snapshot (bảng DatTours từng được tạo tay, ví dụ
            // FK_DatTours_Tours), nên tìm và xóa mọi khóa ngoại, chỉ mục trên DatTours.TourId theo cột.
            // Đồng thời bỏ khóa ngoại thừa FK_DatTours_TaiKhoans (trùng với khóa ngoại EF trên TaiKhoanId).
            migrationBuilder.Sql(@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'ALTER TABLE [DatTours] DROP CONSTRAINT [' + f.name + N'];'
FROM sys.foreign_keys f
JOIN sys.foreign_key_columns fc ON fc.constraint_object_id = f.object_id
WHERE f.parent_object_id = OBJECT_ID(N'DatTours') AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = N'TourId';
SELECT @sql += N'DROP INDEX [' + i.name + N'] ON [DatTours];'
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
WHERE i.object_id = OBJECT_ID(N'DatTours') AND i.is_primary_key = 0 AND COL_NAME(ic.object_id, ic.column_id) = N'TourId';
IF OBJECT_ID(N'FK_DatTours_TaiKhoans', N'F') IS NOT NULL
    SET @sql += N'ALTER TABLE [DatTours] DROP CONSTRAINT [FK_DatTours_TaiKhoans];';
EXEC sp_executesql @sql;");

            migrationBuilder.DropForeignKey(
                name: "FK_LichKhoiHanhTours_Tours_TourId",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropForeignKey(
                name: "FK_TinNhanLienHes_TaiKhoans_TaiKhoanId",
                table: "TinNhanLienHes");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "TinNhanLienHes");

            migrationBuilder.DropColumn(
                name: "HoTen",
                table: "TinNhanLienHes");

            migrationBuilder.DropColumn(
                name: "TaiKhoanDangNhap",
                table: "TaiKhoans");

            migrationBuilder.DropColumn(
                name: "HoTen",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "LoaiNhanVien",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "NhanViens");

            migrationBuilder.DropColumn(
                name: "SoLuotDaDung",
                table: "KhuyenMais");

            migrationBuilder.DropColumn(
                name: "MaKhuyenMai",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "NgayDi",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "PhuongThucThanhToan",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "SoNguoi",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "TongTien",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "TongTienTruocGiam",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "TourId",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "TrangThaiThanhToan",
                table: "DatTours");

            migrationBuilder.AlterColumn<int>(
                name: "TaiKhoanId",
                table: "TinNhanLienHes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "LichKhoiHanhTours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "KhuyenMais",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "SoGiayTo",
                table: "HanhKhachDatTours",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LoaiHanhKhach",
                table: "HanhKhachDatTours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LoaiGiayTo",
                table: "HanhKhachDatTours",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "HanhKhachDatTours",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "DatTours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "GiuCho",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "DaXacNhan");

            migrationBuilder.AlterColumn<string>(
                name: "MaChuyenDi",
                table: "DatTours",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "LichKhoiHanhTourId",
                table: "DatTours",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_Email",
                table: "TaiKhoans",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoans_SoDienThoai",
                table: "TaiKhoans",
                column: "SoDienThoai",
                unique: true,
                filter: "[SoDienThoai] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TaiKhoan_DinhDanh",
                table: "TaiKhoans",
                sql: "[Email] IS NOT NULL OR [SoDienThoai] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Lich_SoCho",
                table: "LichKhoiHanhTours",
                sql: "[SoChoCon] >= 0 AND [SoChoCon] <= [SoChoToiDa]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Lich_TrangThai",
                table: "LichKhoiHanhTours",
                sql: "[TrangThai] IN ('MoBan','TamNgung','DaHuy')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_KhuyenMai_TrangThai",
                table: "KhuyenMais",
                sql: "[TrangThai] IN ('HoatDong','TamNgung')");

            migrationBuilder.CreateIndex(
                name: "IX_HanhKhachDatTours_DatTourId_TaiKhoanId",
                table: "HanhKhachDatTours",
                columns: new[] { "DatTourId", "TaiKhoanId" },
                unique: true,
                filter: "[TaiKhoanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HanhKhachDatTours_MaThanhToan",
                table: "HanhKhachDatTours",
                column: "MaThanhToan",
                unique: true,
                filter: "[MaThanhToan] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HanhKhach_GiaLucDat",
                table: "HanhKhachDatTours",
                sql: "[GiaLucDat] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_HanhKhach_Loai",
                table: "HanhKhachDatTours",
                sql: "[LoaiHanhKhach] IN (N'Người lớn', N'Trẻ nhỏ')");

            migrationBuilder.CreateIndex(
                name: "IX_DatTours_MaChuyenDi",
                table: "DatTours",
                column: "MaChuyenDi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatTours_MaMoi",
                table: "DatTours",
                column: "MaMoi",
                unique: true,
                filter: "[MaMoi] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatTours_LoaiDon",
                table: "DatTours",
                sql: "[LoaiDon] IN ('CaNhan','Nhom')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatTours_SoTienGiam",
                table: "DatTours",
                sql: "[SoTienGiam] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DatTours_TrangThai",
                table: "DatTours",
                sql: "[TrangThai] IN ('GiuCho','DaXacNhan','DaHuy')");

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_KhuyenMais_KhuyenMaiId",
                table: "DatTours",
                column: "KhuyenMaiId",
                principalTable: "KhuyenMais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_LichKhoiHanhTours_LichKhoiHanhTourId",
                table: "DatTours",
                column: "LichKhoiHanhTourId",
                principalTable: "LichKhoiHanhTours",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_TaiKhoans_TaiKhoanId",
                table: "DatTours",
                column: "TaiKhoanId",
                principalTable: "TaiKhoans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichKhoiHanhTours_Tours_TourId",
                table: "LichKhoiHanhTours",
                column: "TourId",
                principalTable: "Tours",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TinNhanLienHes_TaiKhoans_TaiKhoanId",
                table: "TinNhanLienHes",
                column: "TaiKhoanId",
                principalTable: "TaiKhoans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Ghế chưa điền thông tin: chuỗi rỗng thành NULL cho đúng nghĩa "chưa có"
            migrationBuilder.Sql(@"
UPDATE HanhKhachDatTours SET HoTen = NULL WHERE HoTen = N'';
UPDATE HanhKhachDatTours SET LoaiGiayTo = NULL WHERE LoaiGiayTo = N'';
UPDATE HanhKhachDatTours SET SoGiayTo = NULL WHERE SoGiayTo = N'';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_KhuyenMais_KhuyenMaiId",
                table: "DatTours");

            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_LichKhoiHanhTours_LichKhoiHanhTourId",
                table: "DatTours");

            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_TaiKhoans_TaiKhoanId",
                table: "DatTours");

            migrationBuilder.DropForeignKey(
                name: "FK_LichKhoiHanhTours_Tours_TourId",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropForeignKey(
                name: "FK_TinNhanLienHes_TaiKhoans_TaiKhoanId",
                table: "TinNhanLienHes");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoans_Email",
                table: "TaiKhoans");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoans_SoDienThoai",
                table: "TaiKhoans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TaiKhoan_DinhDanh",
                table: "TaiKhoans");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Lich_SoCho",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Lich_TrangThai",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_KhuyenMai_TrangThai",
                table: "KhuyenMais");

            migrationBuilder.DropIndex(
                name: "IX_HanhKhachDatTours_DatTourId_TaiKhoanId",
                table: "HanhKhachDatTours");

            migrationBuilder.DropIndex(
                name: "IX_HanhKhachDatTours_MaThanhToan",
                table: "HanhKhachDatTours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HanhKhach_GiaLucDat",
                table: "HanhKhachDatTours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_HanhKhach_Loai",
                table: "HanhKhachDatTours");

            migrationBuilder.DropIndex(
                name: "IX_DatTours_MaChuyenDi",
                table: "DatTours");

            migrationBuilder.DropIndex(
                name: "IX_DatTours_MaMoi",
                table: "DatTours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatTours_LoaiDon",
                table: "DatTours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatTours_SoTienGiam",
                table: "DatTours");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DatTours_TrangThai",
                table: "DatTours");

            migrationBuilder.AlterColumn<int>(
                name: "TaiKhoanId",
                table: "TinNhanLienHes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "TinNhanLienHes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoTen",
                table: "TinNhanLienHes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaiKhoanDangNhap",
                table: "TaiKhoans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HoTen",
                table: "NhanViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoaiNhanVien",
                table: "NhanViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "NhanViens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "LichKhoiHanhTours",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "KhuyenMais",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "SoLuotDaDung",
                table: "KhuyenMais",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "SoGiayTo",
                table: "HanhKhachDatTours",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LoaiHanhKhach",
                table: "HanhKhachDatTours",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "LoaiGiayTo",
                table: "HanhKhachDatTours",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HoTen",
                table: "HanhKhachDatTours",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TrangThai",
                table: "DatTours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DaXacNhan",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "GiuCho");

            migrationBuilder.AlterColumn<string>(
                name: "MaChuyenDi",
                table: "DatTours",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<int>(
                name: "LichKhoiHanhTourId",
                table: "DatTours",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "MaKhuyenMai",
                table: "DatTours",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDi",
                table: "DatTours",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PhuongThucThanhToan",
                table: "DatTours",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SoNguoi",
                table: "DatTours",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TongTien",
                table: "DatTours",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TongTienTruocGiam",
                table: "DatTours",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TourId",
                table: "DatTours",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiThanhToan",
                table: "DatTours",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DatTours_TourId",
                table: "DatTours",
                column: "TourId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_KhuyenMais_KhuyenMaiId",
                table: "DatTours",
                column: "KhuyenMaiId",
                principalTable: "KhuyenMais",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_LichKhoiHanhTours_LichKhoiHanhTourId",
                table: "DatTours",
                column: "LichKhoiHanhTourId",
                principalTable: "LichKhoiHanhTours",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_TaiKhoans_TaiKhoanId",
                table: "DatTours",
                column: "TaiKhoanId",
                principalTable: "TaiKhoans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_Tours_TourId",
                table: "DatTours",
                column: "TourId",
                principalTable: "Tours",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LichKhoiHanhTours_Tours_TourId",
                table: "LichKhoiHanhTours",
                column: "TourId",
                principalTable: "Tours",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TinNhanLienHes_TaiKhoans_TaiKhoanId",
                table: "TinNhanLienHes",
                column: "TaiKhoanId",
                principalTable: "TaiKhoans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
