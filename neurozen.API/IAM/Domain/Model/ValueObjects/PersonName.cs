using System.Globalization;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Text;

namespace neurozen.API.IAM.Domain.Model.ValueObjects;

/// <summary>
///     Full name rules: first name and last name (at least two words), up to 120 characters, made of letters
///     (any alphabet, accents and ñ included), spaces, apostrophes, hyphens and periods. Digits and other
///     symbols are rejected. The name is stored trimmed and with single spaces.
/// </summary>
public static class PersonName
{
    /// <summary>Trims and collapses repeated spaces: "  Ana   Torres " → "Ana Torres".</summary>
    public static string Normalize(string? fullName) => TextNormalization.CollapseWhitespace(fullName);

    public static void Validate(string fullName, string field, ValidationErrors errors)
    {
        errors.AddIf(fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2, field,
            "Ingresa tu nombre y apellido");
        errors.AddIf(fullName.Length > User.FullNameMaxLength, field,
            $"El nombre no puede superar {User.FullNameMaxLength} caracteres");
        errors.AddIf(!fullName.All(IsAllowedCharacter), field,
            "El nombre solo puede contener letras, espacios, apóstrofos, guiones y puntos");
        errors.AddIf(fullName.Length > 0 && !fullName.Any(char.IsLetter), field, "Ingresa un nombre válido");
    }

    /// <summary>"Ana Torres Ruiz" → "Ana T." (used where only an abbreviated name may be shown).</summary>
    public static string Abbreviate(string fullName)
    {
        var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => string.Empty,
            1 => words[0],
            _ => $"{words[0]} {char.ToUpperInvariant(words[1][0])}."
        };
    }

    private static bool IsAllowedCharacter(char c) =>
        char.IsLetter(c) ||
        CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark ||
        c is ' ' or '\'' or '’' or '-' or '.';
}
