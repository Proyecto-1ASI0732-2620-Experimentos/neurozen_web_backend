using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Stress.Domain.Model.Aggregates;

namespace neurozen.API.Stress.Domain.Repositories;

public interface IStressTriggerRepository : IBaseRepository<StressTrigger>
{
    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<StressTrigger>> ListByUserIdAsync(long userId);
}

public interface IAssessmentRepository : IBaseRepository<Assessment>
{
    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<Assessment>> ListByUserIdAsync(long userId);
}

public interface IReportRepository : IBaseRepository<Report>
{
    Task<bool> ExistsByAppointmentIdAsync(long appointmentId);

    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<Report>> ListByPatientIdAsync(long patientId);

    /// <summary>Most recent first.</summary>
    Task<IReadOnlyList<Report>> ListByProfessionalIdAsync(long professionalId);
}
