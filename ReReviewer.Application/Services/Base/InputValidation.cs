using System.ComponentModel.DataAnnotations;
using ReReviewer.Data.Entities.Enums;

namespace ReReviewer.Application.Services.Base;

internal static class InputValidation
{
    private const int MaxMaterialTitleLength = 250;
    private const int MaxMaterialUrlLength = 800;

    public static string RequiredText(string? value, string fieldName) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ValidationException($"{fieldName} must not be empty.")
            : value.Trim();

    public static string? OptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string MaxLength(string value, int maxLength, string fieldName) =>
        value.Length > maxLength
            ? throw new ValidationException($"{fieldName} must be at most {maxLength} characters long.")
            : value;

    // Только абсолютные адреса http и https: ссылки выводятся на сайте, javascript: и прочее туда не попадёт.
    public static string HttpUrl(string? value, int maxLength, string fieldName)
    {
        var url = MaxLength(RequiredText(value, fieldName), maxLength, fieldName);

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? url
            : throw new ValidationException($"{fieldName} must be an http or https address.");
    }

    // Материал, который предлагает пользователь (отдельной заявкой или в отзыве). Длины — как у Materials в базе.
    public static string MaterialTitle(string? value) =>
        MaxLength(RequiredText(value, "Material title"), MaxMaterialTitleLength, "Material title");

    public static string MaterialUrl(string? value) =>
        HttpUrl(value, MaxMaterialUrlLength, "Material link");

    public static MaterialFormat ParseMaterialFormat(string? format) =>
        Enum.TryParse<MaterialFormat>(format, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ValidationException(
                $"Unknown material format '{format}'. Allowed: {string.Join(", ", Enum.GetNames<MaterialFormat>())}.");

    // Id из вложенных моделей (например, жанры игры): при сохранении он обязателен.
    public static List<TKey> RequiredIds<TItem, TKey>(IEnumerable<TItem> items, Func<TItem, TKey?> id, string itemName)
        where TKey : struct =>
        items
            .Select(item => id(item) ?? throw new ValidationException($"{itemName} id is required."))
            .ToList();
}