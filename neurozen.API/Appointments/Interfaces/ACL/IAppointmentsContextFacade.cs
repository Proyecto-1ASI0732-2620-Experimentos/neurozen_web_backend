namespace neurozen.API.Appointments.Interfaces.ACL;

/// <summary>Minimal appointment data for other contexts (no notes).</summary>
public record AppointmentReference(long Id, long PatientId, long ProfessionalId, string Status, bool Reviewed)
{
    /// <summary>PENDING or CONFIRMED.</summary>
    public bool IsActive => Status is "PENDING" or "CONFIRMED";

    public bool IsCompleted => Status == "COMPLETED";
}

public interface IAppointmentsContextFacade
{
    /// <summary>
    ///     UTC start times already taken for a professional in [fromUtc, toUtcExclusive).
    ///     Used by available-slots to hide occupied hours.
    /// </summary>
    Task<IReadOnlyCollection<DateTime>> ListBookedStartTimesAsync(long professionalId, DateTime fromUtc,
        DateTime toUtcExclusive);

    Task<AppointmentReference?> FindAppointmentAsync(long appointmentId);

    /// <summary>Sets appointment.reviewed = true. Does not save: the caller commits in its unit of work.</summary>
    Task MarkReviewedAsync(long appointmentId);
}
