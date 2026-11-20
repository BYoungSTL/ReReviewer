namespace ReReviewer.Application.ViewModels;

public class ReviewCriterionScoreViewModel
{
    public int CriterionId { get; set; }

    // Процент от 0 до 100.
    public byte Score { get; set; }

    #region Только чтение

    public string? CriterionTitle { get; set; }

    #endregion
}