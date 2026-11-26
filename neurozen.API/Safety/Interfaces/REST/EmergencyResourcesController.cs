using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Safety.Domain.Model.Queries;
using neurozen.API.Safety.Domain.Services;
using neurozen.API.Safety.Interfaces.REST.Resources;
using neurozen.API.Safety.Interfaces.REST.Transform;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Safety.Interfaces.REST;

/// <summary>
///     Crisis lines. Public on purpose: a person in crisis must reach them even without a session (or with an expired
///     one). Read-only.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/emergency-resources")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Safety")]
public class EmergencyResourcesController(IEmergencyResourceQueryService queryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Verified emergency phone lines by country (?country=PE, default PE)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Lines (empty list when there is none for the country)",
        typeof(IEnumerable<EmergencyResourceResource>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid country code", typeof(ErrorResponse))]
    public ActionResult<IEnumerable<EmergencyResourceResource>> GetEmergencyResources([FromQuery] string? country)
    {
        var verifiedAt = queryService.VerifiedAt;
        return Ok(queryService.Handle(new GetEmergencyResourcesQuery(country))
            .Select(resource => EmergencyResourceAssembler.ToResource(resource, verifiedAt)));
    }
}
