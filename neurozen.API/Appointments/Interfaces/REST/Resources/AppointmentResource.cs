using System.Text.Json.Serialization;

namespace neurozen.API.Appointments.Interfaces.REST.Resources;

/// <summary>
///     Appointment as the frontend expects it. patientEmail is only sent to the psychologist of the
///     appointment (omitted otherwise). appointmentType is numeric (1, 2, 3). Dates are UTC with "Z".
/// </summary>
public record AppointmentResource(
    long Id,
    long PatientId,
    string PatientName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? PatientEmail,
    long ProfessionalId,
    DateTime AppointmentDateTime,
    int AppointmentType,
    string Status,
    string NotasAdicionales,
    bool Reviewed);

public record CreateAppointmentResource(
    long? PatientId,
    long? ProfessionalId,
    DateTime? AppointmentDateTime,
    int? AppointmentType,
    string? NotasAdicionales);

public record UpdateAppointmentStatusResource(string? Status);

public record AppointmentTypeResource(
    int Value,
    string Name,
    string DisplayName,
    string Description,
    int EstimatedDurationMinutes);
