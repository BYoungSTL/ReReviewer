namespace ReReviewer.Data.Models;

// MaterialIds может быть пустым — тогда отзыв о работе обзорщика в целом.
// CriterionScores: Id критерия → процент от 0 до 100, по каждому критерию (все обязательны).
public record ReviewInput(
    string Text,
    byte Score,
    IReadOnlyCollection<Guid> MaterialIds,
    IReadOnlyDictionary<int, byte> CriterionScores);