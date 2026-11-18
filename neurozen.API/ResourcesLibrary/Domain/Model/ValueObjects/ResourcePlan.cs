namespace neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;

/// <summary>
///     Minimum subscription plan needed to see a resource. The numeric values are the planId of Subscriptions
///     (1 = Básico, 2 = Avanzado, 3 = Profesional).
///     Access rules: Básico (and users without an active subscription) see only BASIC resources (1-2 of each type);
///     Avanzado sees BASIC + ADVANCED (the whole current library); Profesional sees everything, including resources
///     marked PROFESSIONAL. Verified psychologists and admins see everything. A resource above the caller's plan is
///     neither listed nor returned (its contentUrl is never sent).
/// </summary>
public enum ResourcePlan
{
    Basic = 1,
    Advanced = 2,
    Professional = 3
}

public static class ResourcePlanExtensions
{
    public static bool IsValid(int value) => Enum.IsDefined(typeof(ResourcePlan), value);

    /// <summary>Name shown in messages: Básico, Avanzado, Profesional.</summary>
    public static string DisplayName(this ResourcePlan plan) => plan switch
    {
        ResourcePlan.Basic => "Básico",
        ResourcePlan.Advanced => "Avanzado",
        ResourcePlan.Professional => "Profesional",
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, null)
    };
}
