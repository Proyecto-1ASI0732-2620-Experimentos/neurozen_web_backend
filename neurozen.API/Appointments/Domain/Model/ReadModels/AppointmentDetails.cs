using neurozen.API.Appointments.Domain.Model.Aggregates;

namespace neurozen.API.Appointments.Domain.Model.ReadModels;

/// <summary>Appointment plus patient data. PatientEmail is only filled for the psychologist of the appointment.</summary>
public record AppointmentDetails(Appointment Appointment, string PatientName, string? PatientEmail);
