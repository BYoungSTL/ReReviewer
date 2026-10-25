using System.ComponentModel.DataAnnotations;

namespace ReReviewer.Application.Services.Base;

internal static class InputValidation
{
    public static string RequiredText(string? value, string fieldName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ValidationException($"{fieldName} must not be empty.")
            : value.Trim();

    public static string? OptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Id из вложенных моделей (например, жанры игры): при сохранении он обязателен.
    public static List<TKey> RequiredIds<TItem, TKey>(IEnumerable<TItem> items, Func<TItem, TKey?> id, string itemName)
        where TKey : struct =>
        items
            .Select(item => id(item) ?? throw new ValidationException($"{itemName} id is required."))
            .ToList();
}