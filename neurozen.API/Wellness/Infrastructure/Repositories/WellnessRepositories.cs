using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using neurozen.API.Wellness.Domain.Entities;
using neurozen.API.Wellness.Domain.Repositories;

namespace neurozen.API.Wellness.Infrastructure.Repositories;

public class HealthMetricRepository(AppDbContext context)
    : BaseRepository<HealthMetric>(context), IHealthMetricRepository
{
    public async Task<PageResult<HealthMetric>> ListByUserIdAsync(long userId, PageRequest? pageRequest)
    {
        var query = Context.Set<HealthMetric>()
            .AsNoTracking()
            .Where(metric => metric.UserId == userId)
            .OrderByDescending(metric => metric.CreatedAt)
            .ThenByDescending(metric => metric.Id);

        if (pageRequest is null)
            return PageResult<HealthMetric>.Unpaged(await query.ToListAsync());

        var totalCount = await query.CountAsync();
        var items = await query.Skip(pageRequest.Offset).Take(pageRequest.PageSize).ToListAsync();
        return new PageResult<HealthMetric>(items, totalCount);
    }

    public Task<HealthMetric?> FindLatestByUserIdAsync(long userId) =>
        Context.Set<HealthMetric>()
            .AsNoTracking()
            .Where(metric => metric.UserId == userId)
            .OrderByDescending(metric => metric.CreatedAt)
            .ThenByDescending(metric => metric.Id)
            .FirstOrDefaultAsync();
}

public class MeditationRepository(AppDbContext context) : IMeditationRepository
{
    public async Task<IReadOnlyList<Meditation>> ListAllAsync() =>
        await context.Set<Meditation>()
            .AsNoTracking()
            .OrderBy(meditation => meditation.Id)
            .ToListAsync();
}
