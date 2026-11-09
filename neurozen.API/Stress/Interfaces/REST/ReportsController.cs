using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using neurozen.API.Stress.Domain.Model.Queries;
using neurozen.API.Stress.Domain.Services;
using neurozen.API.Stress.Interfaces.REST.Resources;
using neurozen.API.Stress.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Stress.Interfaces.REST;

/// <summary>
///     Progress reports a patient shares with the psychologist of an appointment (consent: the patient decides).
///     Readable only by that patient and that (verified) psychologist.
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Stress - Reports")]
public class ReportsController(
    IReportCommandService commandService,
    IReportQueryService queryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "?patientId= (USER, own) or ?professionalId= (verified psychologist, own), most recent first",
        Description = "Optional page/pageSize pagination; pageSize is capped at 100. Without both parameters, returns the full list.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Reports", typeof(IEnumerable<ReportResource>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Neither or both filters", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not my reports", typeof(ErrorResponse))]
    public async Task<ActionResult<IEnumerable<ReportResource>>> GetReports(
        [FromQuery] long? patientId, [FromQuery] long? professionalId,
        [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var reports = await queryService.Handle(new GetReportsQuery(patientId, professionalId, page, pageSize));
        if (reports.TotalCount is { } totalCount)
            Response.Headers["X-Total-Count"] = totalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Ok(reports.Items.Select(StressResourceAssembler.ToResource));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.User)]
    [SwaggerOperation(Summary = "Share a progress report on one of my PENDING/CONFIRMED appointments (one per appointment)")]
    [SwaggerResponse(StatusCodes.Status201Created, "Shared", typeof(ReportResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data or appointment", typeof(ErrorResponse))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "A report already exists for this appointment", typeof(ErrorResponse))]
    public async Task<ActionResult<ReportResource>> CreateReport([FromBody] CreateReportResource resource)
    {
        var report = await commandService.Handle(StressResourceAssembler.ToCommand(resource));
        return StatusCode(StatusCodes.Status201Created, StressResourceAssembler.ToResource(report));
    }
}
