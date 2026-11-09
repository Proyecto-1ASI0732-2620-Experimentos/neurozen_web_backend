using Microsoft.EntityFrameworkCore;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace neurozen.API.Appointments.Infrastructure.Repositories;

public class AppointmentRepository(AppDbContext context) : BaseRepository<Appointment>(context), IAppointmentRepository
{
    public async Task<IReadOnlyList<Appointment>> ListByPatientIdAsync(long patientId) =>
        await Context.Set<Appointment>()
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.AppointmentDateTime).ThenBy(a => a.Id)
            .ToListAsync();

    public async Task<IReadOnlyList<Appointment>> ListByProfessionalIdAsync(long professionalId) =>
        await Context.Set<Appointment>()
            .Where(a => a.ProfessionalId == professionalId)
            .OrderBy(a => a.AppointmentDateTime).ThenBy(a => a.Id)
            .ToListAsync();

    public Task<PageResult<Appointment>> ListPageByPatientIdAsync(long patientId, PageRequest? pageRequest) =>
        ListPageAsync(Context.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .OrderBy(a => a.AppointmentDateTime).ThenBy(a => a.Id), pageRequest);

    public Task<PageResult<Appointment>> ListPageByProfessionalIdAsync(long professionalId, PageRequest? pageRequest) =>
        ListPageAsync(Context.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.ProfessionalId == professionalId)
            .OrderBy(a => a.AppointmentDateTime).ThenBy(a => a.Id), pageRequest);

    public Task<bool> IsSlotTakenAsync(long professionalId, DateTime startUtc) =>
        Context.Set<Appointment>().AnyAsync(a =>
            a.ProfessionalId == professionalId &&
            a.AppointmentDateTime == startUtc &&
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed));

    private static async Task<PageResult<Appointment>> ListPageAsync(
        IOrderedQueryable<Appointment> query, PageRequest? pageRequest)
    {
        if (pageRequest is null)
            return PageResult<Appointment>.Unpaged(await query.ToListAsync());

        var totalCount = await query.CountAsync();
        var items = await query.Skip(pageRequest.Offset).Take(pageRequest.PageSize).ToListAsync();
        return new PageResult<Appointment>(items, totalCount);
    }
}
