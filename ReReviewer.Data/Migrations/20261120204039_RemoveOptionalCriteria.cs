using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReReviewer.Data.Migrations
{
    // Необязательных (жанровых) критериев больше нет: все критерии обязательны в каждом отзыве.
    // Жанровые критерии удаляются вместе с их оценками в отзывах, а у критерия пропадает жанр (GenreId).
    // Оставшимся критериям, которых не хватает в каком-то отзыве, проставляется общая оценка отзыва в процентах —
    // как в RequireGeneralCriteriaAsPercent; обычно таких нет.
    /// <inheritdoc />
    public partial class RemoveOptionalCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE s FROM ReviewCriterionScores s
                JOIN Criteria c ON c.Id = s.CriterionId
                WHERE c.GenreId IS NOT NULL;

                DELETE FROM Criteria WHERE GenreId IS NOT NULL;

                INSERT INTO ReviewCriterionScores (ReviewId, CriterionId, Score)
                SELECT r.Id, c.Id, r.Score * 10
                FROM Reviews r
                CROSS JOIN Criteria c
                WHERE NOT EXISTS (SELECT 1 FROM ReviewCriterionScores s WHERE s.ReviewId = r.Id AND s.CriterionId = c.Id);
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Criteria_Genres_GenreId",
                table: "Criteria");

            migrationBuilder.DropIndex(
                name: "IX_Criteria_GenreId",
                table: "Criteria");

            migrationBuilder.DropColumn(
                name: "GenreId",
                table: "Criteria");
        }

        // Возвращает колонку GenreId: все критерии становятся общими. Удалённые жанровые критерии и их оценки
        // не восстановить.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenreId",
                table: "Criteria",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_GenreId",
                table: "Criteria",
                column: "GenreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Criteria_Genres_GenreId",
                table: "Criteria",
                column: "GenreId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
