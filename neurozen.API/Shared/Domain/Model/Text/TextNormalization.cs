namespace neurozen.API.Shared.Domain.Model.Text;

/// <summary>Small, culture-independent text helpers shared by every bounded context.</summary>
public static class TextNormalization
{
    /// <summary>Trims the value and collapses any run of whitespace into a single space. null → "".</summary>
    public static string CollapseWhitespace(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
