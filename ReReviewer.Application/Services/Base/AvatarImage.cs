namespace ReReviewer.Application.Services.Base;

// Тип аватарки определяется по первым байтам файла, а не по имени или заголовку запроса:
// API отдаёт её всем, поэтому сохраняется только то, что действительно является картинкой.
internal static class AvatarImage
{
    public const int MaxBytes = 512 * 1024;

    public static string? DetectContentType(ReadOnlySpan<byte> content) =>
        content switch
        {
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => "image/png",
            [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
            [(byte)'R', (byte)'I', (byte)'F', (byte)'F', _, _, _, _, (byte)'W', (byte)'E', (byte)'B', (byte)'P', ..] => "image/webp",
            _ => null
        };
}
