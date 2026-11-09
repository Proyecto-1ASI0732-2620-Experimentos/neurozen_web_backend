using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Domain.Model.ReadModels;

public sealed record NextAppointmentDetails(
    long Id,
    long PatientId,
    long ProfessionalId,
    DateTime AppointmentDateTime,
    int AppointmentType,
    string Status,
    string? NotasAdicionales,
    bool Reviewed);

public sealed record DashboardDetails(HealthMetric? LatestMetric, NextAppointmentDetails? NextAppointment);
