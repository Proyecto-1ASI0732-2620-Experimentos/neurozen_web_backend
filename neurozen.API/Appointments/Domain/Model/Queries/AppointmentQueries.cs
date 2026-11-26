namespace neurozen.API.Appointments.Domain.Model.Queries;

/// <summary>Exactly one of PatientId / ProfessionalId must be given.</summary>
public record GetAppointmentsQuery(
    long? PatientId,
    long? ProfessionalId,
    int? Page = null,
    int? PageSize = null);

public record GetAppointmentByIdQuery(long Id);

/// <summary>
///     GET /appointments/availability: free slots of a professional per date ("YYYY-MM-DD", Lima time).
///     DurationMinutes: block size (null = 60).
/// </summary>
public record GetAppointmentAvailabilityQuery(
    long? ProfessionalId,
    string? StartDate,
    string? EndDate,
    int? DurationMinutes = null);
