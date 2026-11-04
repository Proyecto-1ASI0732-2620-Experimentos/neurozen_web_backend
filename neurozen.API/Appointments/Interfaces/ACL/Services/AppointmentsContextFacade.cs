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

    public async Task<AppointmentReference?> FindAppointmentAsync(long appointmentId)
    {
        var appointment = await context.Set<Appointment>().FindAsync(appointmentId);
        return appointment is null
            ? null
            : new AppointmentReference(appointment.Id, appointment.PatientId, appointment.ProfessionalId,
                appointment.Status.ToWire(), appointment.Reviewed);
    }

    public async Task MarkReviewedAsync(long appointmentId)
    {
        var appointment = await context.Set<Appointment>().FindAsync(appointmentId);
        appointment?.MarkReviewed(DateTime.UtcNow);
    }
}
