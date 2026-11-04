using neurozen.API.Shared.Domain.Model;

namespace neurozen.API.Stress.Domain.Model.ValueObjects;

public enum TriggerCategory
{
    Meeting,
    Deadline,
    InterpersonalConflict,
    WorkOverload,
    TechnicalProblems,
    DifficultClient,
    OrganizationalChanges,
    Other
}

public static class TriggerCategoryExtensions
{
    /// <summary>Wire / storage value in camelCase: meeting, workOverload, ...</summary>
    public static string ToWire(this TriggerCategory category) => EnumWire.ToCamelCase(category);

    public static TriggerCategory FromWire(string value) => EnumWire.Parse<TriggerCategory>(value);

    /// <summary>Unknown or missing categories become "other" (specification rule, not a 400).</summary>
    public static TriggerCategory FromWireOrOther(string? value) =>
        EnumWire.TryParse<TriggerCategory>(value, out var category) ? category : TriggerCategory.Other;
}
