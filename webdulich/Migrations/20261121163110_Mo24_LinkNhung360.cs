using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace webdulich.Migrations
{
    /// <inheritdoc />
    public partial class Mo24_LinkNhung360 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LinkNhung360",
                table: "DiemThamQuans",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_DiemThamQuan_LinkNhung360",
                table: "DiemThamQuans",
                sql: "[LinkNhung360] IS NULL OR [LinkNhung360] LIKE 'https://www.google.com/maps/embed?%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DiemThamQuan_LinkNhung360",
                table: "DiemThamQuans");

            migrationBuilder.DropColumn(
                name: "LinkNhung360",
                table: "DiemThamQuans");
        }
    }
}
