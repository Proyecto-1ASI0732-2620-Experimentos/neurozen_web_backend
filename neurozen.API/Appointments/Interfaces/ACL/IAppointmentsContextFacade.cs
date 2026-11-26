namespace neurozen.API.Appointments.Interfaces.ACL;

/// <summary>Minimal appointment data for other contexts (no notes).</summary>
public record AppointmentReference(long Id, long PatientId, long ProfessionalId, string Status, bool Reviewed)
{
    /// <summary>PENDING or CONFIRMED.</summary>
    public bool IsActive => Status is "PENDING" or "CONFIRMED";

    public bool IsCompleted => Status == "COMPLETED";
}

public record NextAppointmentReference(
    long Id,
    long PatientId,
    long ProfessionalId,
    DateTime AppointmentDateTime,
    int AppointmentType,
    string Status,
    string? NotasAdicionales,
    bool Reviewed);

/// <summary>A PENDING or CONFIRMED appointment as an occupied range: UTC start and duration of its type.</summary>
public record BookedSlotReference(DateTime StartUtc, int DurationMinutes);

public interface IAppointmentsContextFacade
{
    /// <summary>
    ///     PENDING/CONFIRMED appointments of a professional that overlap [fromUtc, toUtcExclusive), each with the
    ///     duration of its type (an appointment that started before fromUtc but is still running is included).
    /// </summary>
    Task<IReadOnlyList<BookedSlotReference>> ListBookedSlotsAsync(long professionalId, DateTime fromUtc,
        DateTime toUtcExclusive);

    /// <summary>
    ///     UTC start times already taken for a professional in [fromUtc, toUtcExclusive).
    ///     Used by available-slots to hide occupied hours.
    /// </summary>
    Task<IReadOnlyCollection<DateTime>> ListBookedStartTimesAsync(long professionalId, DateTime fromUtc,
        DateTime toUtcExclusive);

    Task<AppointmentReference?> FindAppointmentAsync(long appointmentId);

    Task<NextAppointmentReference?> FindNextAppointmentAsync(long userId, DateTime fromUtc);

    /// <summary>Sets appointment.reviewed = true. Does not save: the caller commits in its unit of work.</summary>
    Task MarkReviewedAsync(long appointmentId);
}
