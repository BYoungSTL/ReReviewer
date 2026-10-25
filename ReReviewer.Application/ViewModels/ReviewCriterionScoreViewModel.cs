namespace ReReviewer.Application.ViewModels;

public class ReviewCriterionScoreViewModel
{
    public int CriterionId { get; set; }
    public byte Score { get; set; }

    #region Только чтение

    public string? CriterionTitle { get; set; }

    #endregion
}