namespace neurozen.API.Appointments.Domain.Model.Commands;

public record CreateAppointmentCommand(
    long? PatientId,
    long? ProfessionalId,
    DateTime? AppointmentDateTime,
    int? AppointmentType,
    string? NotasAdicionales);

public record UpdateAppointmentStatusCommand(long AppointmentId, string? Status);
