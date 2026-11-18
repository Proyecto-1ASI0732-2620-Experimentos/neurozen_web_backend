using System.Text.RegularExpressions;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.IAM.Domain.Model.ValueObjects;

/// <summary>
///     Peruvian DNI rules: exactly 8 ASCII digits (no letters, symbols or inner spaces).
///     Only the FORMAT is checked: there is no RENIEC integration and the backend never claims that a DNI is
///     authentic or that it belongs to the person who registered it.
///     The DNI is personal data (Ley 29733): it must never be written to logs or returned in public resources.
/// </summary>
public static partial class Dni
{
    public const int Length = 8;

    /// <summary>Removes leading/trailing whitespace only. Inner spaces are kept so they are rejected.</summary>
    public static string Normalize(string? dni) => (dni ?? string.Empty).Trim();

    public static bool IsValid(string? normalizedDni) =>
        normalizedDni is not null && DniRegex().IsMatch(normalizedDni);

    public static void Validate(string normalizedDni, string field, ValidationErrors errors)
    {
        if (string.IsNullOrEmpty(normalizedDni))
        {
            errors.Add(field, "El DNI es obligatorio");
            return;
        }

        errors.AddIf(!IsValid(normalizedDni), field, $"El DNI debe tener exactamente {Length} dígitos numéricos");
    }

    // [0-9] instead of \d: in .NET \d also matches non-ASCII digits (e.g. Arabic-Indic).
    [GeneratedRegex("^[0-9]{8}$", RegexOptions.CultureInvariant)]
    private static partial Regex DniRegex();
}
