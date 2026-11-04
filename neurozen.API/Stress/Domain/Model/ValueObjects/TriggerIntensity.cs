namespace neurozen.API.Stress.Domain.Model.ValueObjects;

public enum TriggerIntensity
{
    Low,
    Medium,
    High
}

public static class TriggerIntensityExtensions
{
    public static string ToWire(this TriggerIntensity intensity) => intensity.ToString().ToLowerInvariant();

    public static TriggerIntensity FromWire(string value) => Enum.Parse<TriggerIntensity>(value, true);

    /// <summary>Always computed on the server: 1-3 low, 4-6 medium, 7-10 high.</summary>
    public static TriggerIntensity FromStressLevel(int stressLevel) => stressLevel switch
    {
        <= 3 => TriggerIntensity.Low,
        <= 6 => TriggerIntensity.Medium,
        _ => TriggerIntensity.High
    };
}
