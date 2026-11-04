namespace neurozen.API.Shared.Domain.Model;

/// <summary>Enum ↔ wire string helpers for camelCase and lowercase JSON values.</summary>
public static class EnumWire
{
    /// <summary>WorkOverload → "workOverload".</summary>
    public static string ToCamelCase<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    /// <summary>Case-insensitive match against the enum names (numeric strings are rejected).</summary>
    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        foreach (var candidate in Enum.GetValues<TEnum>())
            if (string.Equals(candidate.ToString(), value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                result = candidate;
                return true;
            }

        return false;
    }

    public static TEnum Parse<TEnum>(string value) where TEnum : struct, Enum =>
        TryParse<TEnum>(value, out var result)
            ? result
            : throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown {typeof(TEnum).Name}");
}
