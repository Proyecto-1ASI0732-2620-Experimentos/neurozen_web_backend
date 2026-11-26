using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Appointments.Domain.Repositories;

public interface IAppointmentRepository : IBaseRepository<Appointment>
{
    /// <summary>Ordered by date ascending.</summary>
    Task<IReadOnlyList<Appointment>> ListByPatientIdAsync(long patientId);

    /// <summary>Ordered by date ascending.</summary>
    Task<IReadOnlyList<Appointment>> ListByProfessionalIdAsync(long professionalId);

    /// <summary>Ordered by date ascending; pagination and count are executed by the database.</summary>
    Task<PageResult<Appointment>> ListPageByPatientIdAsync(long patientId, PageRequest? pageRequest);

    /// <summary>Ordered by date ascending; pagination and count are executed by the database.</summary>
    Task<PageResult<Appointment>> ListPageByProfessionalIdAsync(long professionalId, PageRequest? pageRequest);

    /// <summary>True if a PENDING or CONFIRMED appointment already takes this professional's slot.</summary>
    Task<bool> IsSlotTakenAsync(long professionalId, DateTime startUtc);

    /// <summary>
    ///     True if a PENDING or CONFIRMED appointment of the professional overlaps [startUtc, startUtc + duration),
    ///     taking into account the duration of each appointment's type (60, 45 or 30 minutes).
    /// </summary>
    Task<bool> HasOverlappingActiveAppointmentAsync(long professionalId, DateTime startUtc, int durationMinutes);

    /// <summary>
    ///     Inside a transaction, locks the professional's row (SELECT ... FOR UPDATE) so that concurrent bookings for
    ///     the same professional are serialized until commit. No-op on non-relational providers.
    /// </summary>
    Task LockProfessionalScheduleAsync(long professionalId);
}
