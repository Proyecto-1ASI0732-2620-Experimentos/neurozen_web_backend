namespace neurozen.API.Stress.Domain.Model.ValueObjects;

public enum AssessmentLevel
{
    Low,
    Moderate,
    High
}

public static class AssessmentLevelExtensions
{
    public static string ToWire(this AssessmentLevel level) => level.ToString().ToLowerInvariant();

    public static AssessmentLevel FromWire(string value) => Enum.Parse<AssessmentLevel>(value, true);
}
