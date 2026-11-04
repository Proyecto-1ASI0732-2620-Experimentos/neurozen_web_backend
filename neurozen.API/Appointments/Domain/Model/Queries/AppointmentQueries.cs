namespace neurozen.API.Appointments.Domain.Model.Queries;

/// <summary>Exactly one of PatientId / ProfessionalId must be given.</summary>
public record GetAppointmentsQuery(long? PatientId, long? ProfessionalId);

public record GetAppointmentByIdQuery(long Id);
