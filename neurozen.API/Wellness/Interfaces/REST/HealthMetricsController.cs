using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using neurozen.API.Wellness.Domain.Model.Commands;
using neurozen.API.Wellness.Domain.Model.Queries;
using neurozen.API.Wellness.Domain.Services;
using neurozen.API.Wellness.Interfaces.REST.Resources;
using neurozen.API.Wellness.Interfaces.REST.Transform;
using Swashbuckle.AspNetCore.Annotations;

namespace neurozen.API.Wellness.Interfaces.REST;

/// <summary>Health metrics of the account owner. {userId} must be the "sub" of the token (403 otherwise).</summary>
[ApiController]
[Route("api/v1/users/{userId:long}/health_metrics")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Wellness")]
public class HealthMetricsController(
    IHealthMetricCommandService commandService,
    IHealthMetricQueryService queryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(Summary = "List my health metrics, most recent first",
        Description = "Optional page/pageSize pagination; pageSize is capped at 100. Without both parameters, returns the full list.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Health metrics", typeof(IEnumerable<HealthMetricResource>))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not your health metrics")]
    public async Task<ActionResult<IEnumerable<HealthMetricResource>>> GetHealthMetrics(
        long userId, [FromQuery] int? page, [FromQuery] int? pageSize)
    {
        var result = await queryService.Handle(new GetHealthMetricsQuery(userId, page, pageSize));
        if (result.TotalCount is { } totalCount)
            Response.Headers["X-Total-Count"] = totalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Ok(result.Items.Select(WellnessAssemblers.ToResource));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Record a health metric")]
    [SwaggerResponse(StatusCodes.Status201Created, "Created", typeof(HealthMetricResource))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid metric")]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not your health metrics")]
    public async Task<ActionResult<HealthMetricResource>> CreateHealthMetric(
        long userId, [FromBody] CreateHealthMetricResource resource)
    {
        var metric = await commandService.Handle(new CreateHealthMetricCommand(
            userId, resource.StressLevel, resource.HeartRate, resource.SleepHours, resource.Notes));
        var response = WellnessAssemblers.ToResource(metric);
        return CreatedAtAction(nameof(GetHealthMetrics), new { userId }, response);
    }
}
