namespace neurozen.API.Stress.Interfaces.REST.Resources;

public record TriggerResource(
    long Id,
    long UserId,
    string Description,
    string Category,
    int StressLevel,
    string Intensity,
    DateTime TriggeredAt);

/// <summary>"intensity" is not bound: it is computed on the server from stressLevel.</summary>
public record CreateTriggerResource(
    long? UserId,
    string? Description,
    string? Category,
    int? StressLevel,
    DateTime? TriggeredAt);

public record AssessmentResource(
    long Id,
    long UserId,
    IReadOnlyList<int> Answers,
    int Score,
    string Level,
    DateTime CompletedAt);

/// <summary>score, level and completedAt are not bound: they are computed on the server.</summary>
public record CreateAssessmentResource(long? UserId, List<int>? Answers);

public record LastAssessmentResource(int Score, string Level, DateTime? CompletedAt);

public record ReportResource(
    long Id,
    long PatientId,
    string PatientName,
    long ProfessionalId,
    long AppointmentId,
    decimal StressCurrent,
    decimal StressAverage,
    decimal WeeklyChange,
    string PeakHours,
    int TriggersCount,
    string TopCategory,
    LastAssessmentResource? LastAssessment,
    string Note,
    DateTime CreatedAt);

/// <summary>lastAssessment.level and createdAt are not bound: the level is derived from the score, the date is the server's.</summary>
public record LastAssessmentInputResource(int? Score, DateTime? CompletedAt);

public record CreateReportResource(
    long? PatientId,
    long? ProfessionalId,
    long? AppointmentId,
    decimal? StressCurrent,
    decimal? StressAverage,
    decimal? WeeklyChange,
    string? PeakHours,
    int? TriggersCount,
    string? TopCategory,
    LastAssessmentInputResource? LastAssessment,
    string? Note);
