namespace ReReviewer.Data.Entities;

public class ReviewCriterionScore
{
    public Guid ReviewId { get; set; }
    public int CriterionId { get; set; }

    // Процент от 0 до 100, который читатель поставил обзорщику по этому критерию.
    public byte Score { get; set; }

    #region Virtual Properties

    public virtual Review Review { get; set; } = null!;
    public virtual Criterion Criterion { get; set; } = null!;

    #endregion
}