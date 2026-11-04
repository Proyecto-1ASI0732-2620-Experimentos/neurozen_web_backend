using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.Json;

/// <summary>
///     Dates travel as ISO 8601 in UTC with a trailing "Z" ("2026-10-20T14:00:00Z").
///     Input with an offset is converted to UTC; input without offset is taken as UTC.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string OutputFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text) ||
            !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                out var value))
            throw new JsonException("Fecha no válida. Usa ISO 8601 en UTC, por ejemplo 2026-10-20T14:00:00Z");

        return value.UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
        writer.WriteStringValue(utc.ToString(OutputFormat, CultureInfo.InvariantCulture));
    }
}
