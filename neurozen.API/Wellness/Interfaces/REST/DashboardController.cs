using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Interfaces.REST;

[ApiController]
[Route("api/v1/users/{userId:guid}/dashboard")]
public class DashboardController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard(Guid userId)
    {
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