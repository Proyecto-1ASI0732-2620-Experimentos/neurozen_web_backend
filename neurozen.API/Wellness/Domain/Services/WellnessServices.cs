using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Domain.Model.Commands;
using neurozen.API.Wellness.Domain.Model.Queries;
using neurozen.API.Wellness.Domain.Model.ReadModels;

namespace neurozen.API.Wellness.Domain.Services;

public interface IHealthMetricCommandService
{
    Task<HealthMetric> Handle(CreateHealthMetricCommand command);
}

public interface IHealthMetricQueryService
{
    Task<PageResult<HealthMetric>> Handle(GetHealthMetricsQuery query);
}

public interface IDashboardQueryService
{
    Task<DashboardDetails> Handle(GetDashboardQuery query);
}

public interface IMeditationQueryService
{
    Task<IReadOnlyList<Meditation>> Handle(GetMeditationsQuery query);
}
