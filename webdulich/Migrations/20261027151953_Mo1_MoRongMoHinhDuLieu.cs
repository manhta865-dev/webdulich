using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo1_MoRongMoHinhDuLieu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NgungBan",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "TaiKhoans",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoDienThoai",
                table: "TaiKhoans",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoChoToiDa",
                table: "LichKhoiHanhTours",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoDoChoNgoiId",
                table: "LichKhoiHanhTours",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GiaLucDat",
                table: "HanhKhachDatTours",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MaThanhToan",
                table: "HanhKhachDatTours",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayNha",
                table: "HanhKhachDatTours",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaiKhoanId",
                table: "HanhKhachDatTours",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HanGiuCho",
                table: "DatTours",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiDon",
                table: "DatTours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CaNhan");

            migrationBuilder.AddColumn<string>(
                name: "MaMoi",
                table: "DatTours",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDat",
                table: "DatTours",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "DatTours",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DaXacNhan");

            migrationBuilder.CreateTable(
                name: "GiaoDichThanhToans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatTourId = table.Column<int>(type: "int", nullable: true),
                    HanhKhachDatTourId = table.Column<int>(type: "int", nullable: true),
                    NguoiXacNhanId = table.Column<int>(type: "int", nullable: true),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaThamChieuNganHang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayXacNhan = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichThanhToans", x => x.Id);
                    table.CheckConstraint("CK_GiaoDich_MotDoiTuong", "([DatTourId] IS NOT NULL AND [HanhKhachDatTourId] IS NULL) OR ([DatTourId] IS NULL AND [HanhKhachDatTourId] IS NOT NULL)");
                    table.CheckConstraint("CK_GiaoDich_SoTien", "[SoTien] > 0");
                    table.CheckConstraint("CK_GiaoDich_TrangThai", "[TrangThai] IN ('ChoXacNhan','ThanhCong','ThatBai')");
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToans_DatTours_DatTourId",
                        column: x => x.DatTourId,
                        principalTable: "DatTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToans_HanhKhachDatTours_HanhKhachDatTourId",
                        column: x => x.HanhKhachDatTourId,
                        principalTable: "HanhKhachDatTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiaoDichThanhToans_TaiKhoans_NguoiXacNhanId",
                        column: x => x.NguoiXacNhanId,
                        principalTable: "TaiKhoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoDoChoNgois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenSoDo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhuongTienId = table.Column<int>(type: "int", nullable: true),
                    SoHang = table.Column<int>(type: "int", nullable: false),
                    SoCot = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoDoChoNgois", x => x.Id);
                    table.CheckConstraint("CK_SoDoChoNgoi_KichThuoc", "[SoHang] > 0 AND [SoCot] > 0");
                    table.ForeignKey(
                        name: "FK_SoDoChoNgois_PhuongTiens_PhuongTienId",
                        column: x => x.PhuongTienId,
                        principalTable: "PhuongTiens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViTriChoNgois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoDoChoNgoiId = table.Column<int>(type: "int", nullable: false),
                    MaViTri = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Hang = table.Column<int>(type: "int", nullable: false),
                    Cot = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViTriChoNgois", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViTriChoNgois_SoDoChoNgois_SoDoChoNgoiId",
                        column: x => x.SoDoChoNgoiId,
                        principalTable: "SoDoChoNgois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChoNgoiChuyenDis",
                columns: table => new
                {
                    LichKhoiHanhTourId = table.Column<int>(type: "int", nullable: false),
                    ViTriChoNgoiId = table.Column<int>(type: "int", nullable: false),
                    HanhKhachDatTourId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoNgoiChuyenDis", x => new { x.LichKhoiHanhTourId, x.ViTriChoNgoiId });
                    table.ForeignKey(
                        name: "FK_ChoNgoiChuyenDis_HanhKhachDatTours_HanhKhachDatTourId",
                        column: x => x.HanhKhachDatTourId,
                        principalTable: "HanhKhachDatTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChoNgoiChuyenDis_LichKhoiHanhTours_LichKhoiHanhTourId",
                        column: x => x.LichKhoiHanhTourId,
                        principalTable: "LichKhoiHanhTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChoNgoiChuyenDis_ViTriChoNgois_ViTriChoNgoiId",
                        column: x => x.ViTriChoNgoiId,
                        principalTable: "ViTriChoNgois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichKhoiHanhTours_SoDoChoNgoiId",
                table: "LichKhoiHanhTours",
                column: "SoDoChoNgoiId");

            migrationBuilder.CreateIndex(
                name: "IX_HanhKhachDatTours_TaiKhoanId",
                table: "HanhKhachDatTours",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChoNgoiChuyenDis_HanhKhachDatTourId",
                table: "ChoNgoiChuyenDis",
                column: "HanhKhachDatTourId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChoNgoiChuyenDis_ViTriChoNgoiId",
                table: "ChoNgoiChuyenDis",
                column: "ViTriChoNgoiId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToans_DatTourId",
                table: "GiaoDichThanhToans",
                column: "DatTourId",
                unique: true,
                filter: "[TrangThai] = 'ThanhCong' AND [DatTourId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToans_HanhKhachDatTourId",
                table: "GiaoDichThanhToans",
                column: "HanhKhachDatTourId",
                unique: true,
                filter: "[TrangThai] = 'ThanhCong' AND [HanhKhachDatTourId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToans_MaThamChieuNganHang",
                table: "GiaoDichThanhToans",
                column: "MaThamChieuNganHang",
                unique: true,
                filter: "[MaThamChieuNganHang] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichThanhToans_NguoiXacNhanId",
                table: "GiaoDichThanhToans",
                column: "NguoiXacNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_SoDoChoNgois_PhuongTienId",
                table: "SoDoChoNgois",
                column: "PhuongTienId");

            migrationBuilder.CreateIndex(
                name: "IX_ViTriChoNgois_SoDoChoNgoiId_Hang_Cot",
                table: "ViTriChoNgois",
                columns: new[] { "SoDoChoNgoiId", "Hang", "Cot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ViTriChoNgois_SoDoChoNgoiId_MaViTri",
                table: "ViTriChoNgois",
                columns: new[] { "SoDoChoNgoiId", "MaViTri" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HanhKhachDatTours_TaiKhoans_TaiKhoanId",
                table: "HanhKhachDatTours",
                column: "TaiKhoanId",
                principalTable: "TaiKhoans",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LichKhoiHanhTours_SoDoChoNgois_SoDoChoNgoiId",
                table: "LichKhoiHanhTours",
                column: "SoDoChoNgoiId",
                principalTable: "SoDoChoNgois",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HanhKhachDatTours_TaiKhoans_TaiKhoanId",
                table: "HanhKhachDatTours");

            migrationBuilder.DropForeignKey(
                name: "FK_LichKhoiHanhTours_SoDoChoNgois_SoDoChoNgoiId",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropTable(
                name: "ChoNgoiChuyenDis");

            migrationBuilder.DropTable(
                name: "GiaoDichThanhToans");

            migrationBuilder.DropTable(
                name: "ViTriChoNgois");

            migrationBuilder.DropTable(
                name: "SoDoChoNgois");

            migrationBuilder.DropIndex(
                name: "IX_LichKhoiHanhTours_SoDoChoNgoiId",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropIndex(
                name: "IX_HanhKhachDatTours_TaiKhoanId",
                table: "HanhKhachDatTours");

            migrationBuilder.DropColumn(
                name: "NgungBan",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "TaiKhoans");

            migrationBuilder.DropColumn(
                name: "SoDienThoai",
                table: "TaiKhoans");

            migrationBuilder.DropColumn(
                name: "SoChoToiDa",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropColumn(
                name: "SoDoChoNgoiId",
                table: "LichKhoiHanhTours");

            migrationBuilder.DropColumn(
                name: "GiaLucDat",
                table: "HanhKhachDatTours");

            migrationBuilder.DropColumn(
                name: "MaThanhToan",
                table: "HanhKhachDatTours");

            migrationBuilder.DropColumn(
                name: "NgayNha",
                table: "HanhKhachDatTours");

            migrationBuilder.DropColumn(
                name: "TaiKhoanId",
                table: "HanhKhachDatTours");

            migrationBuilder.DropColumn(
                name: "HanGiuCho",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "LoaiDon",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "MaMoi",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "NgayDat",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "DatTours");
        }
    }
}
