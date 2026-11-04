using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.ReadModels;
using neurozen.API.Stress.Domain.Model.ValueObjects;
using neurozen.API.Stress.Interfaces.REST.Resources;

namespace neurozen.API.Stress.Interfaces.REST.Transform;

public static class StressResourceAssembler
{
    public static TriggerResource ToResource(StressTrigger t) => new(
        t.Id, t.UserId, t.Description, t.Category.ToWire(), t.StressLevel, t.Intensity.ToWire(), t.TriggeredAt);

    public static AssessmentResource ToResource(Assessment a) => new(
        a.Id, a.UserId, a.Answers, a.Score, a.Level.ToWire(), a.CompletedAt);

    public static ReportResource ToResource(ReportDetails details)
    {
        var r = details.Report;
        return new ReportResource(
            r.Id,
            r.PatientId,
            details.PatientName,
            r.ProfessionalId,
            r.AppointmentId,
            r.StressCurrent,
            r.StressAverage,
            r.WeeklyChange,
            r.PeakHours ?? string.Empty,
            r.TriggersCount,
            r.TopCategory?.ToWire() ?? string.Empty,
            r.LastAssessmentScore is { } score && r.LastAssessmentLevel is { } level
                ? new LastAssessmentResource(score, level.ToWire(), r.LastAssessmentCompletedAt)
                : null,
            r.Note ?? string.Empty,
            r.CreatedAt);
    }

    public static CreateTriggerCommand ToCommand(CreateTriggerResource r) =>
        new(r.UserId, r.Description, r.Category, r.StressLevel, r.TriggeredAt);

    public static CreateAssessmentCommand ToCommand(CreateAssessmentResource r) => new(r.UserId, r.Answers);

    public static CreateReportCommand ToCommand(CreateReportResource r) =>
        new(r.PatientId, r.ProfessionalId, r.AppointmentId, r.StressCurrent, r.StressAverage, r.WeeklyChange,
            r.PeakHours, r.TriggersCount, r.TopCategory,
            r.LastAssessment is null ? null : new LastAssessmentInput(r.LastAssessment.Score, r.LastAssessment.CompletedAt),
            r.Note);
}
