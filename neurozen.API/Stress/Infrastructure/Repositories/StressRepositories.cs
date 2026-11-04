using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Repositories;

namespace neurozen.API.Stress.Infrastructure.Repositories;

public class StressTriggerRepository(AppDbContext context)
    : BaseRepository<StressTrigger>(context), IStressTriggerRepository
{
    public async Task<IReadOnlyList<StressTrigger>> ListByUserIdAsync(long userId) =>
        await Context.Set<StressTrigger>()
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TriggeredAt).ThenByDescending(t => t.Id)
            .ToListAsync();
}

public class AssessmentRepository(AppDbContext context)
    : BaseRepository<Assessment>(context), IAssessmentRepository
{
    public async Task<IReadOnlyList<Assessment>> ListByUserIdAsync(long userId) =>
        await Context.Set<Assessment>()
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt).ThenByDescending(a => a.Id)
            .ToListAsync();
}

public class ReportRepository(AppDbContext context) : BaseRepository<Report>(context), IReportRepository
{
    public Task<bool> ExistsByAppointmentIdAsync(long appointmentId) =>
        Context.Set<Report>().AnyAsync(r => r.AppointmentId == appointmentId);

    public async Task<IReadOnlyList<Report>> ListByPatientIdAsync(long patientId) =>
        await Context.Set<Report>()
            .AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .ToListAsync();

    public async Task<IReadOnlyList<Report>> ListByProfessionalIdAsync(long professionalId) =>
        await Context.Set<Report>()
            .AsNoTracking()
            .Where(r => r.ProfessionalId == professionalId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .ToListAsync();
}
