using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Queries;
using neurozen.API.Stress.Domain.Model.ReadModels;
using neurozen.API.Stress.Domain.Repositories;
using neurozen.API.Stress.Domain.Services;

namespace neurozen.API.Stress.Application.Internal.QueryServices;

public class StressTriggerQueryService(IStressTriggerRepository triggerRepository, ICurrentUser currentUser)
    : IStressTriggerQueryService
{
    public Task<IReadOnlyList<StressTrigger>> Handle(GetTriggersQuery query) =>
        triggerRepository.ListByUserIdAsync(StressAccess.RequireOwnUserId(currentUser, query.UserId, "userId"));
}

public class AssessmentQueryService(IAssessmentRepository assessmentRepository, ICurrentUser currentUser)
    : IAssessmentQueryService
{
    public Task<IReadOnlyList<Assessment>> Handle(GetAssessmentsQuery query) =>
        assessmentRepository.ListByUserIdAsync(StressAccess.RequireOwnUserId(currentUser, query.UserId, "userId"));
}

/// <summary>
///     A report is readable only by its patient and by the psychologist of the appointment
///     (who must be verified). Nobody else, not even an admin.
/// </summary>
public class ReportQueryService(
    IReportRepository reportRepository,
    IProfessionalsContextFacade professionalsContextFacade,
    IIamContextFacade iamContextFacade,
    ICurrentUser currentUser) : IReportQueryService
{
    private const string ForbiddenMessage = "Solo puedes ver tus propios informes";

    public async Task<IReadOnlyList<ReportDetails>> Handle(GetReportsQuery query)
    {
        if (query.PatientId is null == query.ProfessionalId is null)
            throw new ValidationFailedException("Indica patientId o professionalId (solo uno)");

        IReadOnlyList<Report> reports;
        if (query.PatientId is not null)
        {
            var patientId = StressAccess.RequireOwnUserId(currentUser, query.PatientId, "patientId");
            reports = await reportRepository.ListByPatientIdAsync(patientId);
        }
        else
        {
            if (!currentUser.IsInRole(AppRoles.Psychologist)) throw new ForbiddenOperationException(ForbiddenMessage);

            var professional = await professionalsContextFacade.FindByUserIdAsync(currentUser.Id);
            if (professional is null || professional.Id != query.ProfessionalId!.Value)
                throw new ForbiddenOperationException(ForbiddenMessage);
            if (!professional.IsBookable)
                throw new ForbiddenOperationException("Tu cuenta profesional aún no está verificada");

            reports = await reportRepository.ListByProfessionalIdAsync(professional.Id);
        }

        var patients = await iamContextFacade.FetchUserSummariesAsync(reports.Select(r => r.PatientId));
        return reports
            .Select(r => new ReportDetails(r,
                patients.TryGetValue(r.PatientId, out var patient) ? patient.FullName : string.Empty))
            .ToList();
    }
}
