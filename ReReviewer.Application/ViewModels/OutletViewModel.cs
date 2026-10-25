namespace ReReviewer.Application.ViewModels;

public class OutletViewModel
{
    // Null — новое издание, иначе — правка существующего.
    public Guid? Id { get; set; }

    public string Name { get; set; } = string.Empty;

    #region Только чтение

    public List<ReviewerOutletViewModel> Reviewers { get; set; } = [];

    #endregion
}