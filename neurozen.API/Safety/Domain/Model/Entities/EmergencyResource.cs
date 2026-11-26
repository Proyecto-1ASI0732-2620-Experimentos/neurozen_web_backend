namespace neurozen.API.Safety.Domain.Model.Entities;

/// <summary>
///     A crisis / emergency phone line shown to users. Read-only reference data: only lines the team has verified with
///     the official source may be listed (see <see cref="EmergencyResourceCatalog" />).
/// </summary>
public record EmergencyResource(string Name, string Phone, string Description, string Country);

/// <summary>
///     The only lines NeuroZen shows today (the same ones the web frontend already displays). Do not add numbers or
///     institutions without a documented verification against the official source (gob.pe / MINSA).
/// </summary>
public static class EmergencyResourceCatalog
{
    public const string Peru = "PE";

    public static readonly IReadOnlyList<EmergencyResource> All =
    [
        new("Línea 113, opción 5 (salud mental)", "113",
            "Línea del Ministerio de Salud del Perú. Marca 113 y elige la opción 5 para orientación en salud mental.",
            Peru),
        new("SAMU 106", "106",
            "Sistema de Atención Móvil de Urgencias del Ministerio de Salud del Perú, para emergencias médicas.",
            Peru)
    ];
}
