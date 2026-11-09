using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.Queries;
using neurozen.API.Stress.Domain.Model.ReadModels;
using neurozen.API.Shared.Domain.Model.Pagination;

namespace neurozen.API.Stress.Domain.Services;

public interface IStressTriggerCommandService
{
    Task<StressTrigger> Handle(CreateTriggerCommand command);

    /// <summary>404 when it does not exist or belongs to another user.</summary>
    Task Handle(DeleteTriggerCommand command);
}

public interface IStressTriggerQueryService
{
    Task<PageResult<StressTrigger>> Handle(GetTriggersQuery query);
}

public interface IAssessmentCommandService
{
    Task<Assessment> Handle(CreateAssessmentCommand command);
}

public interface IAssessmentQueryService
{
    Task<PageResult<Assessment>> Handle(GetAssessmentsQuery query);
}

public interface IReportCommandService
{
    Task<ReportDetails> Handle(CreateReportCommand command);
}

public interface IReportQueryService
{
    Task<PageResult<ReportDetails>> Handle(GetReportsQuery query);
}
