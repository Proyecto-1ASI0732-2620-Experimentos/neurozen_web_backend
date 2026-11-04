using neurozen.API.Stress.Domain.Model.ValueObjects;

namespace neurozen.API.Stress.Domain.Model.Aggregates;

/// <summary>A stressful situation recorded by a user. Health data: its content is never logged.</summary>
public class StressTrigger
{
    public const int DescriptionMinLength = 5;
    public const int DescriptionMaxLength = 500;
    public const int MinStressLevel = 1;
    public const int MaxStressLevel = 10;

    protected StressTrigger()
    {
        Description = string.Empty;
    }

    public StressTrigger(long userId, string description, TriggerCategory category, int stressLevel,
        DateTime triggeredAt)
    {
        UserId = userId;
        Description = description;
        Category = category;
        StressLevel = stressLevel;
        Intensity = TriggerIntensityExtensions.FromStressLevel(stressLevel);
        TriggeredAt = triggeredAt;
        CreatedAt = DateTime.UtcNow;
    }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string Description { get; private set; }
    public TriggerCategory Category { get; private set; }
    public int StressLevel { get; private set; }

    /// <summary>Derived from <see cref="StressLevel" />; the client value is never used.</summary>
    public TriggerIntensity Intensity { get; private set; }

    public DateTime TriggeredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
