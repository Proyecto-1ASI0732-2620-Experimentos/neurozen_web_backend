using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Appointments.Domain.Model.Aggregates;

/// <summary>
///     Session between a patient (User) and a Professional, with its state machine:
///     PENDING → CONFIRMED (psychologist, future) · PENDING/CONFIRMED → CANCELLED (either, future) ·
///     CONFIRMED → COMPLETED (psychologist, after the session time). CANCELLED and COMPLETED are final.
///     Who may perform each transition is checked by the application service; the aggregate enforces
///     the states and the time conditions.
/// </summary>
public class Appointment
{
    public const int NotesMaxLength = 500;

    protected Appointment()
    {
    }

    public Appointment(long patientId, long professionalId, DateTime startUtc, EAppointmentType appointmentType,
        string? notasAdicionales)
    {
        PatientId = patientId;
        ProfessionalId = professionalId;
        AppointmentDateTime = startUtc;
        AppointmentType = appointmentType;
        NotasAdicionales = notasAdicionales;
        Status = AppointmentStatus.Pending;
        Reviewed = false;
        ActiveSlot = startUtc;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public long Id { get; private set; }
    public long PatientId { get; private set; }
    public long ProfessionalId { get; private set; }

    /// <summary>Start of the session, UTC.</summary>
    public DateTime AppointmentDateTime { get; private set; }

    public EAppointmentType AppointmentType { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public string? NotasAdicionales { get; private set; }
    public bool Reviewed { get; private set; }

    /// <summary>
    ///     Equal to <see cref="AppointmentDateTime" /> while the appointment occupies the slot, null once
    ///     CANCELLED. A unique index on (ProfessionalId, ActiveSlot) prevents double booking, including
    ///     concurrent requests (MySQL unique indexes allow several NULLs).
    /// </summary>
    public DateTime? ActiveSlot { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>Concurrency token: two simultaneous status changes cannot both win.</summary>
    public DateTime UpdatedAt { get; private set; }

    public bool OccupiesSlot => Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;

    public void Confirm(DateTime nowUtc)
    {
        if (Status != AppointmentStatus.Pending)
            throw new ValidationFailedException("Solo se puede confirmar una cita pendiente");
        if (AppointmentDateTime <= nowUtc)
            throw new ValidationFailedException("No se puede confirmar una cita cuya fecha ya pasó");

        Status = AppointmentStatus.Confirmed;
        UpdatedAt = nowUtc;
    }

    public void Cancel(DateTime nowUtc)
    {
        if (!OccupiesSlot)
            throw new ValidationFailedException(Status == AppointmentStatus.Cancelled
                ? "La cita ya está cancelada"
                : "No se puede cancelar una cita ya atendida");
        if (AppointmentDateTime <= nowUtc)
            throw new ValidationFailedException("No se puede cancelar una cita cuya fecha ya pasó");

        Status = AppointmentStatus.Cancelled;
        ActiveSlot = null;
        UpdatedAt = nowUtc;
    }

    public void Complete(DateTime nowUtc)
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new ValidationFailedException("Solo se puede marcar como atendida una cita confirmada");
        if (AppointmentDateTime > nowUtc)
            throw new ValidationFailedException("La cita aún no ha ocurrido");

        Status = AppointmentStatus.Completed;
        UpdatedAt = nowUtc;
    }

    /// <summary>Set when the patient reviews the session (module 6).</summary>
    public void MarkReviewed(DateTime nowUtc)
    {
        Reviewed = true;
        UpdatedAt = nowUtc;
    }

    /// <summary>Account deletion of the patient: the notes are personal data.</summary>
    public void ClearNotes(DateTime nowUtc)
    {
        if (NotasAdicionales is null) return;
        NotasAdicionales = null;
        UpdatedAt = nowUtc;
    }
}
