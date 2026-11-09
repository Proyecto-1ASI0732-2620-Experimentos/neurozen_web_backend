namespace neurozen.API.Wellness.Interfaces.REST.Resources;

public record HealthMetricResource(
    long Id,
    long UserId,
    int StressLevel,
    int? HeartRate,
    decimal? SleepHours,
    string? Notes,
    DateTime CreatedAt);

public record CreateHealthMetricResource(
    int StressLevel,
    int? HeartRate,
    decimal? SleepHours,
    string? Notes);

public record DashboardResource(
    int? StressLevel,
    int? HeartRate,
    decimal? SleepHours,
    NextAppointmentResource? NextAppointment);

public record NextAppointmentResource(
    long Id,
    long PatientId,
    long ProfessionalId,
    DateTime AppointmentDateTime,
    int AppointmentType,
    string Status,
    string? NotasAdicionales,
    bool Reviewed);

public record MeditationResource(
    long Id,
    string Title,
    string Description,
    int DurationMinutes,
    string ImageUrl,
    string AudioUrl);
