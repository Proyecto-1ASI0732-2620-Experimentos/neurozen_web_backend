using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Interfaces.REST;

/// <summary>Health metrics of the account owner. {userId} must be the "sub" of the token (403 otherwise).</summary>
[ApiController]
[Route("api/v1/users/{userId:long}/health_metrics")]
public class HealthMetricsController(AppDbContext context, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHealthMetrics(long userId)
    {
        WellnessOwnership.Ensure(currentUser, userId);

        var metrics = await context.Set<HealthMetric>()
            .AsNoTracking()
            .Where(metric => metric.UserId == userId)
            .OrderByDescending(metric => metric.CreatedAt)
            .ToListAsync();

        return Ok(metrics.Select(ToResource));
    }

    [HttpPost]
    public async Task<IActionResult> CreateHealthMetric(long userId, [FromBody] CreateHealthMetricRequest request)
    {
        WellnessOwnership.Ensure(currentUser, userId);

        var errors = new ValidationErrors();
        errors.AddIf(request.StressLevel is < 1 or > 10, "stressLevel", "El nivel de estrés debe estar entre 1 y 10");
        errors.AddIf(request.HeartRate is <= 0 or > 250, "heartRate", "La frecuencia cardiaca debe estar entre 1 y 250");
        errors.AddIf(request.SleepHours is < 0 or > 24, "sleepHours", "Las horas de sueño deben estar entre 0 y 24");
        errors.AddIf(request.Notes?.Length > HealthMetric.NotesMaxLength, "notes",
            $"Las notas no pueden superar {HealthMetric.NotesMaxLength} caracteres");
        errors.ThrowIfAny();

        var metric = new HealthMetric(userId, request.StressLevel, request.HeartRate, request.SleepHours, request.Notes);
        context.Add(metric);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetHealthMetrics), new { userId }, ToResource(metric));
    }

    private static object ToResource(HealthMetric metric) => new
    {
        id = metric.Id,
        userId = metric.UserId,
        stressLevel = metric.StressLevel,
        heartRate = metric.HeartRate,
        sleepHours = metric.SleepHours,
        notes = metric.Notes,
        createdAt = metric.CreatedAt
    };
}

public record CreateHealthMetricRequest(
    int StressLevel,
    int? HeartRate,
    decimal? SleepHours,
    string? Notes);

internal static class WellnessOwnership
{
    public static void Ensure(ICurrentUser currentUser, long userId)
    {
        if (userId != currentUser.Id)
            throw new ForbiddenOperationException("No tienes permiso para acceder a estos datos");
    }
}
