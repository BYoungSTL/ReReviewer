using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReReviewer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewToMaterialSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReviewId",
                table: "MaterialSuggestions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSuggestions_ReviewId",
                table: "MaterialSuggestions",
                column: "ReviewId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialSuggestions_Reviews_ReviewId",
                table: "MaterialSuggestions",
                column: "ReviewId",
                principalTable: "Reviews",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialSuggestions_Reviews_ReviewId",
                table: "MaterialSuggestions");

            migrationBuilder.DropIndex(
                name: "IX_MaterialSuggestions_ReviewId",
                table: "MaterialSuggestions");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "MaterialSuggestions");
        }
    }
}
