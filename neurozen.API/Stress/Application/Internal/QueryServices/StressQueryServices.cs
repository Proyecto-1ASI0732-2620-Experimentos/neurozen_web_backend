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
using neurozen.API.Shared.Domain.Model.Pagination;

namespace neurozen.API.Stress.Application.Internal.QueryServices;

public class StressTriggerQueryService(IStressTriggerRepository triggerRepository, ICurrentUser currentUser)
    : IStressTriggerQueryService
{
    public Task<PageResult<StressTrigger>> Handle(GetTriggersQuery query) =>
        triggerRepository.ListByUserIdAsync(
            StressAccess.RequireOwnUserId(currentUser, query.UserId, "userId"),
            PageRequest.From(query.Page, query.PageSize));
}

public class AssessmentQueryService(IAssessmentRepository assessmentRepository, ICurrentUser currentUser)
    : IAssessmentQueryService
{
    public Task<PageResult<Assessment>> Handle(GetAssessmentsQuery query) =>
        assessmentRepository.ListByUserIdAsync(
            StressAccess.RequireOwnUserId(currentUser, query.UserId, "userId"),
            PageRequest.From(query.Page, query.PageSize));
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

    public async Task<PageResult<ReportDetails>> Handle(GetReportsQuery query)
    {
        if (query.PatientId is null == query.ProfessionalId is null)
            throw new ValidationFailedException("Indica patientId o professionalId (solo uno)");

        PageResult<Report> page;
        if (query.PatientId is not null)
        {
            var patientId = StressAccess.RequireOwnUserId(currentUser, query.PatientId, "patientId");
            page = await reportRepository.ListByPatientIdAsync(patientId, PageRequest.From(query.Page, query.PageSize));
        }
        else
        {
            if (!currentUser.IsInRole(AppRoles.Psychologist)) throw new ForbiddenOperationException(ForbiddenMessage);

            var professional = await professionalsContextFacade.FindByUserIdAsync(currentUser.Id);
            if (professional is null || professional.Id != query.ProfessionalId!.Value)
                throw new ForbiddenOperationException(ForbiddenMessage);
            if (!professional.IsBookable)
                throw new ForbiddenOperationException("Tu cuenta profesional aún no está verificada");

            page = await reportRepository.ListByProfessionalIdAsync(professional.Id,
                PageRequest.From(query.Page, query.PageSize));
        }

        var patients = await iamContextFacade.FetchUserSummariesAsync(page.Items.Select(r => r.PatientId));
        var items = page.Items
            .Select(r => new ReportDetails(r,
                patients.TryGetValue(r.PatientId, out var patient) ? patient.FullName : string.Empty))
            .ToList();
        return new PageResult<ReportDetails>(items, page.TotalCount);
    }
}
