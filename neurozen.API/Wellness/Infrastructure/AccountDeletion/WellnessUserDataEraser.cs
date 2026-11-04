using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Application.Accounts;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Wellness.Domain.Entities;

namespace neurozen.API.Wellness.Infrastructure.AccountDeletion;

/// <summary>Deletes the user's health metrics (health data, no reason to keep them).</summary>
public class WellnessUserDataEraser(AppDbContext context) : IUserDataEraser
{
    public int Order => 100;

    public async Task EraseAsync(long userId, CancellationToken cancellationToken = default)
    {
        await context.Set<HealthMetric>()
            .Where(metric => metric.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
