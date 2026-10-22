namespace ReReviewer.Application.ViewModels;

public class RoleViewModel
{
    // Null — новая роль, иначе — правка существующей.
    public int? Id { get; set; }

    public string Title { get; set; } = string.Empty;
}