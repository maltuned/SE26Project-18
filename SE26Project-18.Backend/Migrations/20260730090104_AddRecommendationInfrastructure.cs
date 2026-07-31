using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SE26Project_18.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AppliedEmbeddingVersion",
                table: "users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AppliedEmbeddingVersion",
                table: "recruitments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AppliedEmbeddingVersion",
                table: "games",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "embedding_sync_outbox",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EventId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Target = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    LeaseId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    LeaseExpiresAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    PublishAttempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_embedding_sync_outbox", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "recruitment_views",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RecruitmentId = table.Column<long>(type: "bigint", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recruitment_views", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recruitment_views_recruitments_RecruitmentId",
                        column: x => x.RecruitmentId,
                        principalTable: "recruitments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recruitment_views_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_embedding_sync_outbox_PublishedAt_LeaseExpiresAt",
                table: "embedding_sync_outbox",
                columns: new[] { "PublishedAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_recruitment_views_RecruitmentId",
                table: "recruitment_views",
                column: "RecruitmentId");

            migrationBuilder.CreateIndex(
                name: "IX_recruitment_views_UserId_RecruitmentId",
                table: "recruitment_views",
                columns: new[] { "UserId", "RecruitmentId" },
                unique: true);

            migrationBuilder.Sql(
                "INSERT INTO `embedding_sync_outbox` (`EventId`, `Target`, `EntityId`, `CreatedAt`, `PublishAttempts`) " +
                "SELECT UUID(), 0, `Id`, UTC_TIMESTAMP(6), 0 FROM `users`;");
            migrationBuilder.Sql(
                "INSERT INTO `embedding_sync_outbox` (`EventId`, `Target`, `EntityId`, `CreatedAt`, `PublishAttempts`) " +
                "SELECT UUID(), 1, `Id`, UTC_TIMESTAMP(6), 0 FROM `games`;");
            migrationBuilder.Sql(
                "INSERT INTO `embedding_sync_outbox` (`EventId`, `Target`, `EntityId`, `CreatedAt`, `PublishAttempts`) " +
                "SELECT UUID(), 2, `Id`, UTC_TIMESTAMP(6), 0 FROM `recruitments`;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "embedding_sync_outbox");

            migrationBuilder.DropTable(
                name: "recruitment_views");

            migrationBuilder.DropColumn(
                name: "AppliedEmbeddingVersion",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AppliedEmbeddingVersion",
                table: "recruitments");

            migrationBuilder.DropColumn(
                name: "AppliedEmbeddingVersion",
                table: "games");
        }
    }
}
