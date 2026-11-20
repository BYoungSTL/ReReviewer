using ReReviewer.Data.Entities.BaseEntity;

namespace ReReviewer.Data.Entities;

// Критерий, по которому читатель оценивает обзорщика процентом от 0 до 100. Все критерии обязательны в каждом отзыве
// и всегда видны в рейтинге обзорщика.
public class Criterion : Entity<int>
{
    public string Title { get; set; } = string.Empty;

    #region Virtual Properties

    public virtual ICollection<ReviewCriterionScore> Scores { get; set; } = [];

    #endregion
}
