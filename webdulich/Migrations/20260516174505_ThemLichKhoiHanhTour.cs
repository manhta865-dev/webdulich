using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class ThemLichKhoiHanhTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LichKhoiHanhTourId",
                table: "DatTours",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LichKhoiHanhTours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TourId = table.Column<int>(type: "int", nullable: false),
                    NgayKhoiHanh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioKhoiHanh = table.Column<TimeSpan>(type: "time", nullable: true),
                    SoChoCon = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichKhoiHanhTours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichKhoiHanhTours_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatTours_LichKhoiHanhTourId",
                table: "DatTours",
                column: "LichKhoiHanhTourId");

            migrationBuilder.CreateIndex(
                name: "IX_LichKhoiHanhTours_TourId",
                table: "LichKhoiHanhTours",
                column: "TourId");

            migrationBuilder.AddForeignKey(
                name: "FK_DatTours_LichKhoiHanhTours_LichKhoiHanhTourId",
                table: "DatTours",
                column: "LichKhoiHanhTourId",
                principalTable: "LichKhoiHanhTours",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DatTours_LichKhoiHanhTours_LichKhoiHanhTourId",
                table: "DatTours");

            migrationBuilder.DropTable(
                name: "LichKhoiHanhTours");

            migrationBuilder.DropIndex(
                name: "IX_DatTours_LichKhoiHanhTourId",
                table: "DatTours");

            migrationBuilder.DropColumn(
                name: "LichKhoiHanhTourId",
                table: "DatTours");
        }
    }
}
