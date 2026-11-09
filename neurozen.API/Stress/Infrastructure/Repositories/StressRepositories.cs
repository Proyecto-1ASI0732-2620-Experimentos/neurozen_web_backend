using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Repositories;

namespace neurozen.API.Stress.Infrastructure.Repositories;

public class StressTriggerRepository(AppDbContext context)
    : BaseRepository<StressTrigger>(context), IStressTriggerRepository
{
    public async Task<PageResult<StressTrigger>> ListByUserIdAsync(long userId, PageRequest? pageRequest)
    {
        var query = Context.Set<StressTrigger>()
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TriggeredAt).ThenByDescending(t => t.Id);
        return await StressRepositoryPaging.ToPageAsync(query, pageRequest);
    }
}

public class AssessmentRepository(AppDbContext context)
    : BaseRepository<Assessment>(context), IAssessmentRepository
{
    public async Task<PageResult<Assessment>> ListByUserIdAsync(long userId, PageRequest? pageRequest)
    {
        var query = Context.Set<Assessment>()
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt).ThenByDescending(a => a.Id);
        return await StressRepositoryPaging.ToPageAsync(query, pageRequest);
    }
}

public class ReportRepository(AppDbContext context) : BaseRepository<Report>(context), IReportRepository
{
    public Task<bool> ExistsByAppointmentIdAsync(long appointmentId) =>
        Context.Set<Report>().AnyAsync(r => r.AppointmentId == appointmentId);

    public async Task<PageResult<Report>> ListByPatientIdAsync(long patientId, PageRequest? pageRequest)
    {
        var query = Context.Set<Report>()
            .AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);
        return await StressRepositoryPaging.ToPageAsync(query, pageRequest);
    }

    public async Task<PageResult<Report>> ListByProfessionalIdAsync(long professionalId, PageRequest? pageRequest)
    {
        var query = Context.Set<Report>()
            .AsNoTracking()
            .Where(r => r.ProfessionalId == professionalId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);
        return await StressRepositoryPaging.ToPageAsync(query, pageRequest);
    }
}

internal static class StressRepositoryPaging
{
    public static async Task<PageResult<TEntity>> ToPageAsync<TEntity>(
        IQueryable<TEntity> query, PageRequest? pageRequest)
    {
        if (pageRequest is null)
            return PageResult<TEntity>.Unpaged(await query.ToListAsync());

        var totalCount = await query.CountAsync();
        var items = await query.Skip(pageRequest.Offset).Take(pageRequest.PageSize).ToListAsync();
        return new PageResult<TEntity>(items, totalCount);
    }
}
