namespace ReReviewer.Data.Models;

// MaterialIds может быть пустым — тогда отзыв о работе обзорщика в целом.
// CriterionScores: Id критерия → балл от 1 до 10.
public record ReviewInput(
    string Text,
    byte Score,
    IReadOnlyCollection<Guid> MaterialIds,
    IReadOnlyDictionary<int, byte> CriterionScores);