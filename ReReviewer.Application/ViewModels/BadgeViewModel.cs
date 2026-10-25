namespace ReReviewer.Application.ViewModels;

public class BadgeViewModel
{
    // Null — новый бейдж, иначе — правка существующего.
    public int? Id { get; set; }

    public string Title { get; set; } = string.Empty;
}