using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Interfaces.REST;

[ApiController]
[Route("api/v1/users/{userId:guid}/health_metrics")]
public class HealthMetricsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHealthMetrics(Guid userId)
    {
        var metrics = await context.Set<HealthMetric>()
            .AsNoTracking()
            .Where(metric => metric.UserId == userId)
            .OrderByDescending(metric => metric.CreatedAt)
            .Select(metric => new
            {
                id = metric.Id,
                userId = metric.UserId,
                stressLevel = metric.StressLevel,
                heartRate = metric.HeartRate,
                sleepHours = metric.SleepHours,
                notes = metric.Notes,
                createdAt = metric.CreatedAt
            })
            .ToListAsync();

        return Ok(metrics);
    }

    [HttpPost]
    public async Task<IActionResult> CreateHealthMetric(Guid userId, [FromBody] CreateHealthMetricRequest request)
    {
        if (request.StressLevel is < 1 or > 10)
            return BadRequest(new { message = "StressLevel must be between 1 and 10" });

        if (request.HeartRate is <= 0 or > 250)
            return BadRequest(new { message = "HeartRate must be between 1 and 250" });

        if (request.SleepHours is < 0 or > 24)
            return BadRequest(new { message = "SleepHours must be between 0 and 24" });

        var metric = new HealthMetric(userId, request.StressLevel, request.HeartRate, request.SleepHours, request.Notes);
        context.Add(metric);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHealthMetrics), new { userId }, new
        {
            id = metric.Id,
            userId = metric.UserId,
            stressLevel = metric.StressLevel,
            heartRate = metric.HeartRate,
            sleepHours = metric.SleepHours,
            notes = metric.Notes,
            createdAt = metric.CreatedAt
        });
    }
}

public record CreateHealthMetricRequest(
    int StressLevel,
    int? HeartRate,
    decimal? SleepHours,
    string? Notes);