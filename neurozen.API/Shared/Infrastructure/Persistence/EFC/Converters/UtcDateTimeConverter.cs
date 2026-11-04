using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace neurozen.API.Shared.Infrastructure.Persistence.EFC.Converters;

/// <summary>
///     MySQL DATETIME has no time zone. Everything is stored in UTC and read back
///     with DateTimeKind.Utc so the JSON output always carries "Z".
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
        : value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
