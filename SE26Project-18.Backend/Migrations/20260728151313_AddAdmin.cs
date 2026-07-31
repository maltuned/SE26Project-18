using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE26Project_18.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HandledAt",
                table: "reports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HandledByAdminId",
                table: "reports",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "reports",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "HandledAt",
                table: "feedbacks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HandledByAdminId",
                table: "feedbacks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "feedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "admins",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admins", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_admins_Username",
                table: "admins",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admins");

            migrationBuilder.DropColumn(
                name: "HandledAt",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "HandledByAdminId",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "HandledAt",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "HandledByAdminId",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "feedbacks");
        }
    }
}
