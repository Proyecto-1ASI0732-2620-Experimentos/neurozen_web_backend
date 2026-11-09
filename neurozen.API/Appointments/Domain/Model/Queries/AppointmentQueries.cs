namespace neurozen.API.Appointments.Domain.Model.Queries;

/// <summary>Exactly one of PatientId / ProfessionalId must be given.</summary>
public record GetAppointmentsQuery(
    long? PatientId,
    long? ProfessionalId,
    int? Page = null,
    int? PageSize = null);

public record GetAppointmentByIdQuery(long Id);
