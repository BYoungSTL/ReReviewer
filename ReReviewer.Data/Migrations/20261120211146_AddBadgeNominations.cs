using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReReviewer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeNominations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BadgeNominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BadgeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BadgeNominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BadgeNominations_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BadgeNominations_Reviewers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Reviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BadgeNominations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeNominations_BadgeId",
                table: "BadgeNominations",
                column: "BadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_BadgeNominations_ReviewerId_BadgeId",
                table: "BadgeNominations",
                columns: new[] { "ReviewerId", "BadgeId" });

            migrationBuilder.CreateIndex(
                name: "IX_BadgeNominations_UserId_ReviewerId_BadgeId",
                table: "BadgeNominations",
                columns: new[] { "UserId", "ReviewerId", "BadgeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BadgeNominations");
        }
    }
}
