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

/// <summary>Free slots of one date: date "YYYY-MM-DD" and slots "HH:mm" (Lima time).</summary>
public record AvailabilityDayResource(string Date, IReadOnlyList<string> Slots);

/// <summary>GET /appointments/availability response. nextAvailableDate may be after endDate, or null.</summary>
public record AvailabilityResource(
    long ProfessionalId,
    string TimeZone,
    int SlotMinutes,
    IReadOnlyList<AvailabilityDayResource> Days,
    string? NextAvailableDate);

public record AppointmentTypeResource(
    int Value,
    string Name,
    string DisplayName,
    string Description,
    int EstimatedDurationMinutes);
