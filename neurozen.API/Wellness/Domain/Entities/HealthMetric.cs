namespace neurozen.API.Wellness.Domain.Entities;

public class HealthMetric
{
    private HealthMetric()
    {
        Notes = string.Empty;
    }

    public int Id { get; private set; }
    public Guid UserId { get; private set; }
    public int StressLevel { get; private set; }
    public int? HeartRate { get; private set; }
    public decimal? SleepHours { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public HealthMetric(Guid userId, int stressLevel, int? heartRate, decimal? sleepHours, string? notes)
    {
        UserId = userId;
        StressLevel = stressLevel;
        HeartRate = heartRate;
        SleepHours = sleepHours;
        Notes = notes;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}