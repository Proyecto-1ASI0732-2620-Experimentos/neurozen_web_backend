using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Interfaces.REST;

/// <summary>Dashboard of the account owner. {userId} must be the "sub" of the token (403 otherwise).</summary>
[ApiController]
[Route("api/v1/users/{userId:long}/dashboard")]
public class DashboardController(AppDbContext context, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard(long userId)
    {
        WellnessOwnership.Ensure(currentUser, userId);

        var latestMetric = await context.Set<HealthMetric>()
            .AsNoTracking()
            .Where(metric => metric.UserId == userId)
            .OrderByDescending(metric => metric.CreatedAt)
            .FirstOrDefaultAsync();

        return Ok(new
        {
            stressLevel = latestMetric?.StressLevel,
            heartRate = latestMetric?.HeartRate,
            sleepHours = latestMetric?.SleepHours,
            nextAppointment = (object?)null
        });
    }
}
