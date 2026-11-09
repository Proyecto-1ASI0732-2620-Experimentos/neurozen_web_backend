using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Pagination;
using neurozen.API.Stress.Domain.Model.Aggregates;

namespace neurozen.API.Stress.Domain.Repositories;

public interface IStressTriggerRepository : IBaseRepository<StressTrigger>
{
    /// <summary>Most recent first.</summary>
    Task<PageResult<StressTrigger>> ListByUserIdAsync(long userId, PageRequest? pageRequest);
}

public interface IAssessmentRepository : IBaseRepository<Assessment>
{
    /// <summary>Most recent first.</summary>
    Task<PageResult<Assessment>> ListByUserIdAsync(long userId, PageRequest? pageRequest);
}

public interface IReportRepository : IBaseRepository<Report>
{
    Task<bool> ExistsByAppointmentIdAsync(long appointmentId);

    /// <summary>Most recent first.</summary>
    Task<PageResult<Report>> ListByPatientIdAsync(long patientId, PageRequest? pageRequest);

    /// <summary>Most recent first.</summary>
    Task<PageResult<Report>> ListByProfessionalIdAsync(long professionalId, PageRequest? pageRequest);
}
