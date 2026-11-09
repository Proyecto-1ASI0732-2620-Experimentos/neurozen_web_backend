using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Wellness.Domain.Model.Queries;
using neurozen.API.Wellness.Domain.Services;
using neurozen.API.Wellness.Interfaces.REST.Resources;
using neurozen.API.Wellness.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Wellness.Interfaces.REST;

/// <summary>Dashboard of the account owner. {userId} must be the "sub" of the token (403 otherwise).</summary>
[ApiController]
[Route("api/v1/users/{userId:long}/dashboard")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Wellness")]
public class DashboardController(IDashboardQueryService dashboardQueryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "Latest health metric and next appointment for the account owner")]
    [SwaggerResponse(StatusCodes.Status200OK, "Dashboard", typeof(DashboardResource))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not your dashboard")]
    public async Task<ActionResult<DashboardResource>> GetDashboard(long userId)
    {
        var details = await dashboardQueryService.Handle(new GetDashboardQuery(userId));
        return Ok(WellnessAssemblers.ToResource(details));
    }
}
