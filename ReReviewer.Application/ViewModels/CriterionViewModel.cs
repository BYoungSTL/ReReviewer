namespace ReReviewer.Application.ViewModels;

// Критерий оценки обзорщика. Все критерии обязательны в каждом отзыве.
public class CriterionViewModel
{
    // Null — новый критерий, иначе — правка существующего.
    public int? Id { get; set; }

    public string Title { get; set; } = string.Empty;
}
