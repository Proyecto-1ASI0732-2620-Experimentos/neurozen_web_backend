using Microsoft.EntityFrameworkCore;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace neurozen.API.Appointments.Interfaces.ACL.Services;

public class AppointmentsContextFacade(AppDbContext context) : IAppointmentsContextFacade
{
    /// <summary>Only PENDING and CONFIRMED appointments occupy a slot.</summary>
    public async Task<IReadOnlyCollection<DateTime>> ListBookedStartTimesAsync(long professionalId, DateTime fromUtc,
        DateTime toUtcExclusive) =>
        await context.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.ProfessionalId == professionalId &&
                        (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                        a.AppointmentDateTime >= fromUtc &&
                        a.AppointmentDateTime < toUtcExclusive)
            .Select(a => a.AppointmentDateTime)
            .ToListAsync();

    public async Task<IReadOnlyList<BookedSlotReference>> ListBookedSlotsAsync(long professionalId, DateTime fromUtc,
        DateTime toUtcExclusive)
    {
        var earliestStart = fromUtc.AddMinutes(-AppointmentType.MaxDurationMinutes);
        var active = await context.Set<Appointment>()
            .AsNoTracking()
            .Where(a => a.ProfessionalId == professionalId &&
                        (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed) &&
                        a.AppointmentDateTime > earliestStart &&
                        a.AppointmentDateTime < toUtcExclusive)
            .Select(a => new { a.AppointmentDateTime, a.AppointmentType })
            .ToListAsync();

        return active
            .Select(a => new BookedSlotReference(a.AppointmentDateTime, AppointmentType.DurationOf(a.AppointmentType)))
            .Where(slot => slot.StartUtc.AddMinutes(slot.DurationMinutes) > fromUtc)
            .OrderBy(slot => slot.StartUtc)
            .ToList();
    }

    public async Task<AppointmentReference?> FindAppointmentAsync(long appointmentId)
    {
        var appointment = await context.Set<Appointment>().FindAsync(appointmentId);
        return appointment is null
            ? null
            : new AppointmentReference(appointment.Id, appointment.PatientId, appointment.ProfessionalId,
                appointment.Status.ToWire(), appointment.Reviewed);
    }

    public async Task<NextAppointmentReference?> FindNextAppointmentAsync(long userId, DateTime fromUtc)
    {
        var appointment = await context.Set<Appointment>()
            .AsNoTracking()
            .Where(a => (a.PatientId == userId || a.ProfessionalId == userId) &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.AppointmentDateTime >= fromUtc)
            .OrderBy(a => a.AppointmentDateTime)
            .ThenBy(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.PatientId,
                a.ProfessionalId,
                a.AppointmentDateTime,
                a.AppointmentType,
                a.Status,
                a.NotasAdicionales,
                a.Reviewed
            })
            .FirstOrDefaultAsync();

        return appointment is null
            ? null
            : new NextAppointmentReference(
                appointment.Id,
                appointment.PatientId,
                appointment.ProfessionalId,
                appointment.AppointmentDateTime,
                (int)appointment.AppointmentType,
                appointment.Status.ToString(),
                appointment.NotasAdicionales,
                appointment.Reviewed);
    }

    public async Task MarkReviewedAsync(long appointmentId)
    {
        var appointment = await context.Set<Appointment>().FindAsync(appointmentId);
        appointment?.MarkReviewed(DateTime.UtcNow);
    }
}
