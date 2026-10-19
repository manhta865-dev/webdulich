using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class ThemBangHanhKhachDatTour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HanhKhachDatTours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatTourId = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiGiayTo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoGiayTo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiHanhKhach = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HanhKhachDatTours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HanhKhachDatTours_DatTours_DatTourId",
                        column: x => x.DatTourId,
                        principalTable: "DatTours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HanhKhachDatTours_DatTourId",
                table: "HanhKhachDatTours",
                column: "DatTourId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HanhKhachDatTours");
        }
    }
}
