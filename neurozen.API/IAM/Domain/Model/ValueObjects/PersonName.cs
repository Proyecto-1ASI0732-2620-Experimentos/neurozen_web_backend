using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.IAM.Domain.Model.ValueObjects;

/// <summary>Full name rules: first name and last name (at least two words), up to 120 characters.</summary>
public static class PersonName
{
    public static void Validate(string fullName, string field, ValidationErrors errors)
    {
        errors.AddIf(fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2, field,
            "Ingresa tu nombre y apellido");
        errors.AddIf(fullName.Length > User.FullNameMaxLength, field,
            $"El nombre no puede superar {User.FullNameMaxLength} caracteres");
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
}
