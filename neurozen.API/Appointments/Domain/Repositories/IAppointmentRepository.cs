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
}
