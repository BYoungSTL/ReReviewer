using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.Data.Models;

// MaterialIds может быть пустым — тогда отзыв о работе обзорщика в целом.
// CriterionScores: Id критерия → процент от 0 до 100, по каждому критерию (все обязательны).
// NewMaterials — материалы, которых у обзорщика ещё нет: они проверяются вместе с отзывом и при его одобрении
// становятся материалами обзорщика, отмеченными в отзыве. Null — без новых материалов.
public record ReviewInput(
    string Text,
    byte Score,
    IReadOnlyCollection<Guid> MaterialIds,
    IReadOnlyDictionary<int, byte> CriterionScores,
    IReadOnlyList<NewMaterialInput>? NewMaterials = null);

// Текст уже проверен и обрезан в Application: ссылка — абсолютный адрес http(s).
public record NewMaterialInput(
    Guid GameId,
    string Title,
    string Url,
    MaterialFormat Format,
    DateOnly? PublishedAt);
