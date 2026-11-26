using System.Globalization;
using System.Text;
using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;

namespace neurozen.API.ResourcesLibrary.Domain.Services;

/// <summary>
///     Accent- and case-insensitive relevance of a resource for a set of search terms (pure domain logic).
///     Each term contributes its best match: tag (3) or title (3) weigh more than description (1); a whole-word match
///     in the title adds 1. The total is normalized to 0-1 by the maximum reachable score.
/// </summary>
public static class ResourceRelevanceScorer
{
    private const double TagWeight = 3;
    private const double TitleWeight = 3;
    private const double TitleWholeWordBonus = 1;
    private const double DescriptionWeight = 1;
    private const double MaxPerTerm = TitleWeight + TitleWholeWordBonus;
    private const int MinTermLength = 2;

    private static readonly char[] Separators =
        [' ', ',', '.', ';', ':', '!', '?', '¿', '¡', '(', ')', '"', '\'', '-', '/', '\n', '\t'];

    /// <summary>Lower case, without diacritics (á → a, ñ → n), whitespace collapsed.</summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        return string.Join(' ', builder.ToString().Normalize(NormalizationForm.FormC)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Distinct normalized words of the free text (2+ characters) plus the normalized tags.</summary>
    public static IReadOnlyList<string> Terms(string? query, IEnumerable<string>? tags)
    {
        var words = Normalize(query)
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= MinTermLength);
        var tagTerms = (tags ?? []).Select(Normalize).Where(tag => tag.Length > 0);
        return words.Concat(tagTerms).Distinct().ToList();
    }

    public static double Score(ResourceLibrary resource, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0) return 0;

        var title = Normalize(resource.Title);
        var titleWords = title.Split(Separators, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var description = Normalize(resource.Description);
        var tags = resource.Tags.Select(Normalize).ToList();

        double total = 0;
        foreach (var term in terms)
        {
            double best = 0;
            if (tags.Exists(tag => tag.Contains(term, StringComparison.Ordinal))) best = TagWeight;
            if (title.Contains(term, StringComparison.Ordinal))
                best = Math.Max(best, TitleWeight + (titleWords.Contains(term) ? TitleWholeWordBonus : 0));
            if (best == 0 && description.Contains(term, StringComparison.Ordinal)) best = DescriptionWeight;
            total += best;
        }

        return Math.Round(total / (terms.Count * MaxPerTerm), 2, MidpointRounding.AwayFromZero);
    }
}
