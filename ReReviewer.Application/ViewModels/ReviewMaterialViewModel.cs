namespace ReReviewer.Application.ViewModels;

public class ReviewMaterialViewModel
{
    public Guid Id { get; set; }

    #region Только чтение

    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Format { get; set; }

    #endregion
}