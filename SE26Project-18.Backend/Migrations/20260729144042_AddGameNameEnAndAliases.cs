using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE26Project_18.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGameNameEnAndAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aliases",
                table: "games",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "games",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "games");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "games");
        }
    }
}
