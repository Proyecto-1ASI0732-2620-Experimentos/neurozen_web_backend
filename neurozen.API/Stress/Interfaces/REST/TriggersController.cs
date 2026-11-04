using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.Queries;
using neurozen.API.Stress.Domain.Services;
using neurozen.API.Stress.Interfaces.REST.Resources;
using neurozen.API.Stress.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Stress.Interfaces.REST;

/// <summary>Stress records (health data). USER only, own records only.</summary>
[ApiController]
[Route("api/v1/triggers")]
[Authorize(Roles = AppRoles.User)]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Stress - Triggers")]
public class TriggersController(
    IStressTriggerCommandService commandService,
    IStressTriggerQueryService queryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "My stress records (?userId= must be my id), most recent first")]
    [SwaggerResponse(StatusCodes.Status200OK, "Records", typeof(IEnumerable<TriggerResource>))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not my id", typeof(ErrorResponse))]
    public async Task<ActionResult<IEnumerable<TriggerResource>>> GetTriggers([FromQuery] long? userId)
    {
        var triggers = await queryService.Handle(new GetTriggersQuery(userId));
        return Ok(triggers.Select(StressResourceAssembler.ToResource));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Record a stressful situation",
        Description = "intensity is computed from stressLevel (1-3 low, 4-6 medium, 7-10 high). " +
                      "An unknown category is stored as \"other\". triggeredAt cannot be in the future.")]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(TriggerResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid data", typeof(ErrorResponse))]
    public async Task<ActionResult<TriggerResource>> CreateTrigger([FromBody] CreateTriggerResource resource)
    {
        var trigger = await commandService.Handle(StressResourceAssembler.ToCommand(resource));
        return StatusCode(StatusCodes.Status201Created, StressResourceAssembler.ToResource(trigger));
    }

    [HttpDelete("{id:long}")]
    [SwaggerOperation(Summary = "Delete one of my stress records")]
    [SwaggerResponse(StatusCodes.Status204NoContent, "Deleted")]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found or not mine", typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteTrigger(long id)
    {
        await commandService.Handle(new DeleteTriggerCommand(id));
        return NoContent();
    }
}
