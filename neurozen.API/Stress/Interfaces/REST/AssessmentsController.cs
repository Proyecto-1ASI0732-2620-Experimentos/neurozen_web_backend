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

/// <summary>Self-assessment based on the PSS-10. Guidance only, not a clinical diagnosis. USER only.</summary>
[ApiController]
[Route("api/v1/assessments")]
[Authorize(Roles = AppRoles.User)]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Stress - Assessments")]
public class AssessmentsController(
    IAssessmentCommandService commandService,
    IAssessmentQueryService queryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "My assessments (?userId= must be my id), most recent first")]
    [SwaggerResponse(StatusCodes.Status200OK, "Assessments", typeof(IEnumerable<AssessmentResource>))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not my id", typeof(ErrorResponse))]
    public async Task<ActionResult<IEnumerable<AssessmentResource>>> GetAssessments([FromQuery] long? userId)
    {
        var assessments = await queryService.Handle(new GetAssessmentsQuery(userId));
        return Ok(assessments.Select(StressResourceAssembler.ToResource));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Submit the 10 answers (0-4)",
        Description = "Score and level are computed on the server; questions 4, 5, 7 and 8 are reverse-scored.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Computed result", typeof(AssessmentResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Not exactly 10 answers between 0 and 4", typeof(ErrorResponse))]
    public async Task<ActionResult<AssessmentResource>> CreateAssessment(
        [FromBody] CreateAssessmentResource resource)
    {
        var assessment = await commandService.Handle(StressResourceAssembler.ToCommand(resource));
        return StatusCode(StatusCodes.Status201Created, StressResourceAssembler.ToResource(assessment));
    }
}
