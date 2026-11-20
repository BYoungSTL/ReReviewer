using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ReReviewer.Data.Conversions;

// Все даты в базе — UTC: их пишет SYSUTCDATETIME() или код через DateTime.UtcNow. Но datetime2 часовой пояс не хранит,
// и без конвертера EF читает их с DateTimeKind.Unspecified. Тогда JSON уходит без «Z», браузер принимает время
// за местное, и дата у читателя в другом часовом поясе сдвигается на сутки.
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
