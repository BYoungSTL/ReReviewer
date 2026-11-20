using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReReviewer.Data.Migrations
{
    // Оценка по критерию теперь — процент от 0 до 100, а общие критерии (без жанра) обязательны в каждом отзыве.
    // Старые баллы 1–10 переводятся в проценты (×10). В существующие отзывы, где общих критериев не хватает,
    // проставляется общая оценка отзыва в процентах: отзыв на 7 — 70 % по каждому недостающему критерию.
    /// <inheritdoc />
    public partial class RequireGeneralCriteriaAsPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ReviewCriterionScores_Score",
                table: "ReviewCriterionScores");

            migrationBuilder.Sql("UPDATE ReviewCriterionScores SET Score = Score * 10;");

            migrationBuilder.Sql("""
                INSERT INTO ReviewCriterionScores (ReviewId, CriterionId, Score)
                SELECT r.Id, c.Id, r.Score * 10
                FROM Reviews r
                CROSS JOIN Criteria c
                WHERE c.GenreId IS NULL
                  AND NOT EXISTS (SELECT 1 FROM ReviewCriterionScores s WHERE s.ReviewId = r.Id AND s.CriterionId = c.Id);
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReviewCriterionScores_Score",
                table: "ReviewCriterionScores",
                sql: "[Score] BETWEEN 0 AND 100");
        }

        // Проценты возвращаются к баллам 1–10 с округлением. Оценки, которые проставила Up, не отличить
        // от поставленных читателями, поэтому они остаются: со старой схемой они совместимы.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ReviewCriterionScores_Score",
                table: "ReviewCriterionScores");

            migrationBuilder.Sql("""
                UPDATE ReviewCriterionScores
                SET Score = CASE WHEN ROUND(Score / 10.0, 0) < 1 THEN 1 ELSE CAST(ROUND(Score / 10.0, 0) AS tinyint) END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReviewCriterionScores_Score",
                table: "ReviewCriterionScores",
                sql: "[Score] BETWEEN 1 AND 10");
        }
    }
}
