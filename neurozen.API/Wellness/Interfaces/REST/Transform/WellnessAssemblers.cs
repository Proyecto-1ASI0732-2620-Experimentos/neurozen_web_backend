using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Domain.Model.ReadModels;
using neurozen.API.Wellness.Interfaces.REST.Resources;

namespace neurozen.API.Wellness.Interfaces.REST.Transform;

public static class WellnessAssemblers
{
    public static HealthMetricResource ToResource(HealthMetric metric) => new(
        metric.Id,
        metric.UserId,
        metric.StressLevel,
        metric.HeartRate,
        metric.SleepHours,
        metric.Notes,
        metric.CreatedAt);

    public static DashboardResource ToResource(DashboardDetails details) => new(
        details.LatestMetric?.StressLevel,
        details.LatestMetric?.HeartRate,
        details.LatestMetric?.SleepHours,
        details.NextAppointment is { } appointment
            ? new NextAppointmentResource(
                appointment.Id,
                appointment.PatientId,
                appointment.ProfessionalId,
                appointment.AppointmentDateTime,
                appointment.AppointmentType,
                appointment.Status,
                appointment.NotasAdicionales,
                appointment.Reviewed)
            : null);

    public static MeditationResource ToResource(Meditation meditation) => new(
        meditation.Id,
        meditation.Title,
        meditation.Description,
        meditation.DurationMinutes,
        meditation.ImageUrl,
        meditation.AudioUrl);
}
