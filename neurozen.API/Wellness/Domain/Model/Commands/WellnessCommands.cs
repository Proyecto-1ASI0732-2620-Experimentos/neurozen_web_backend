namespace neurozen.API.Wellness.Domain.Model.Commands;

public record CreateHealthMetricCommand(
    long UserId,
    int StressLevel,
    int? HeartRate,
    decimal? SleepHours,
    string? Notes);
