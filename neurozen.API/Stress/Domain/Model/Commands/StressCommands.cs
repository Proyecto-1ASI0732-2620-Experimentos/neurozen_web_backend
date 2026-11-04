namespace neurozen.API.Stress.Domain.Model.Commands;

/// <summary>"intensity" is not part of the command: it is computed from StressLevel.</summary>
public record CreateTriggerCommand(
    long? UserId,
    string? Description,
    string? Category,
    int? StressLevel,
    DateTime? TriggeredAt);

public record DeleteTriggerCommand(long TriggerId);

/// <summary>score, level and completedAt from the client are ignored.</summary>
public record CreateAssessmentCommand(long? UserId, IReadOnlyList<int>? Answers);

public record LastAssessmentInput(int? Score, DateTime? CompletedAt);

public record CreateReportCommand(
    long? PatientId,
    long? ProfessionalId,
    long? AppointmentId,
    decimal? StressCurrent,
    decimal? StressAverage,
    decimal? WeeklyChange,
    string? PeakHours,
    int? TriggersCount,
    string? TopCategory,
    LastAssessmentInput? LastAssessment,
    string? Note);
