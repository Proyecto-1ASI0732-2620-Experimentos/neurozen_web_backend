using neurozen.API.Stress.Domain.Model.ValueObjects;

namespace neurozen.API.Stress.Domain.Model.Aggregates;

/// <summary>
///     Progress report the patient decides to share with the psychologist of one of their active appointments
///     (one report per appointment). Only the patient and that psychologist can read it.
/// </summary>
public class Report
{
    public const int NoteMaxLength = 500;
    public const int PeakHoursMaxLength = 30;

    protected Report()
    {
    }

    public Report(long patientId, long professionalId, long appointmentId, decimal stressCurrent,
        decimal stressAverage, decimal weeklyChange, string? peakHours, int triggersCount,
        TriggerCategory? topCategory, int? lastAssessmentScore, DateTime? lastAssessmentCompletedAt, string? note)
    {
        PatientId = patientId;
        ProfessionalId = professionalId;
        AppointmentId = appointmentId;
        StressCurrent = stressCurrent;
        StressAverage = stressAverage;
        WeeklyChange = weeklyChange;
        PeakHours = peakHours;
        TriggersCount = triggersCount;
        TopCategory = topCategory;
        LastAssessmentScore = lastAssessmentScore;
        LastAssessmentLevel = lastAssessmentScore is null
            ? null
            : PerceivedStressScale.LevelFor(lastAssessmentScore.Value);
        LastAssessmentCompletedAt = lastAssessmentScore is null ? null : lastAssessmentCompletedAt;
        Note = note;
        CreatedAt = DateTime.UtcNow;
    }

    public long Id { get; private set; }
    public long PatientId { get; private set; }
    public long ProfessionalId { get; private set; }
    public long AppointmentId { get; private set; }
    public decimal StressCurrent { get; private set; }
    public decimal StressAverage { get; private set; }
    public decimal WeeklyChange { get; private set; }
    public string? PeakHours { get; private set; }
    public int TriggersCount { get; private set; }
    public TriggerCategory? TopCategory { get; private set; }
    public int? LastAssessmentScore { get; private set; }

    /// <summary>Recomputed from the score, so it is always consistent with it.</summary>
    public AssessmentLevel? LastAssessmentLevel { get; private set; }

    public DateTime? LastAssessmentCompletedAt { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
