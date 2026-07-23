using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE26Project_18.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddResponseChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChatId",
                table: "responses",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_responses_ChatId",
                table: "responses",
                column: "ChatId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_responses_chats_ChatId",
                table: "responses",
                column: "ChatId",
                principalTable: "chats",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_responses_chats_ChatId",
                table: "responses");

            migrationBuilder.DropIndex(
                name: "IX_responses_ChatId",
                table: "responses");

            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "responses");
        }
    }
}
