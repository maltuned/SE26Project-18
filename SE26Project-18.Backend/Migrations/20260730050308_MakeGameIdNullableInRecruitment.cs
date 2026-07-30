using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE26Project_18.Backend.Migrations
{
    /// <inheritdoc />
    public partial class MakeGameIdNullableInRecruitment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recruitments_games_GameId",
                table: "recruitments");

            migrationBuilder.AlterColumn<long>(
                name: "GameId",
                table: "recruitments",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "GameName",
                table: "recruitments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_recruitments_games_GameId",
                table: "recruitments",
                column: "GameId",
                principalTable: "games",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recruitments_games_GameId",
                table: "recruitments");

            migrationBuilder.DropColumn(
                name: "GameName",
                table: "recruitments");

            migrationBuilder.AlterColumn<long>(
                name: "GameId",
                table: "recruitments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_recruitments_games_GameId",
                table: "recruitments",
                column: "GameId",
                principalTable: "games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
