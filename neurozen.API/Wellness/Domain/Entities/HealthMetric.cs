namespace neurozen.API.Wellness.Domain.Entities;

public class HealthMetric
{
    public const int NotesMaxLength = 1000;

    private HealthMetric()
    {
    }

    public HealthMetric(long userId, int stressLevel, int? heartRate, decimal? sleepHours, string? notes)
    {
        UserId = userId;
        StressLevel = stressLevel;
        HeartRate = heartRate;
        SleepHours = sleepHours;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public int StressLevel { get; private set; }
    public int? HeartRate { get; private set; }
    public decimal? SleepHours { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
