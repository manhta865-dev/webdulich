using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo13_Video : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaYouTube = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    DangHien = table.Column<bool>(type: "bit", nullable: false),
                    TourId = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                    table.CheckConstraint("CK_Video_MaYouTube", "LEN([MaYouTube]) = 11 AND [MaYouTube] NOT LIKE '%[^A-Za-z0-9_-]%'");
                    table.ForeignKey(
                        name: "FK_Videos_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Videos_MaYouTube",
                table: "Videos",
                column: "MaYouTube",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_TourId",
                table: "Videos",
                column: "TourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Videos");
        }
    }
}
