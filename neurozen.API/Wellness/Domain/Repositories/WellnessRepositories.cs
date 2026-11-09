using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Domain.Repositories;

public interface IHealthMetricRepository : IBaseRepository<HealthMetric>
{
    Task<PageResult<HealthMetric>> ListByUserIdAsync(long userId, PageRequest? pageRequest);

    Task<HealthMetric?> FindLatestByUserIdAsync(long userId);
}

public interface IMeditationRepository
{
    Task<IReadOnlyList<Meditation>> ListAllAsync();
}
