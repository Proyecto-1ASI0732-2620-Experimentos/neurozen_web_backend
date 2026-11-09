using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Domain.Model.Queries;
using neurozen.API.Wellness.Domain.Model.ReadModels;
using neurozen.API.Wellness.Domain.Repositories;
using neurozen.API.Wellness.Domain.Services;

namespace neurozen.API.Wellness.Application.Internal.QueryServices;

public class HealthMetricQueryService(IHealthMetricRepository healthMetricRepository, ICurrentUser currentUser)
    : IHealthMetricQueryService
{
    public Task<PageResult<HealthMetric>> Handle(GetHealthMetricsQuery query) =>
        healthMetricRepository.ListByUserIdAsync(
            WellnessAccess.RequireOwnUserId(currentUser, query.UserId),
            PageRequest.From(query.Page, query.PageSize));
}

public class DashboardQueryService(
    IHealthMetricRepository healthMetricRepository,
    IAppointmentsContextFacade appointmentsContextFacade,
    ICurrentUser currentUser) : IDashboardQueryService
{
    public async Task<DashboardDetails> Handle(GetDashboardQuery query)
    {
        var userId = WellnessAccess.RequireOwnUserId(currentUser, query.UserId);
        var latestMetric = await healthMetricRepository.FindLatestByUserIdAsync(userId);
        var appointment = await appointmentsContextFacade.FindNextAppointmentAsync(userId, DateTime.UtcNow);
        var nextAppointment = appointment is null
            ? null
            : new NextAppointmentDetails(
                appointment.Id,
                appointment.PatientId,
                appointment.ProfessionalId,
                appointment.AppointmentDateTime,
                appointment.AppointmentType,
                appointment.Status,
                appointment.NotasAdicionales,
                appointment.Reviewed);
        return new DashboardDetails(latestMetric, nextAppointment);
    }
}

public class MeditationQueryService(IMeditationRepository meditationRepository) : IMeditationQueryService
{
    public Task<IReadOnlyList<Meditation>> Handle(GetMeditationsQuery query) =>
        meditationRepository.ListAllAsync();
}
