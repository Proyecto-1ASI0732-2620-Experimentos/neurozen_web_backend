using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Application.Accounts;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Stress.Domain.Model.Aggregates;

namespace neurozen.API.Stress.Infrastructure.AccountDeletion;

/// <summary>
///     Account deletion: removes the user's stress records, assessments and the reports they shared
///     (health data, no reason to keep them). Reports received by a psychologist belong to their patients
///     and are not touched when the psychologist deletes their account.
/// </summary>
public class StressUserDataEraser(AppDbContext context) : IUserDataEraser
{
    public int Order => 300;

    public async Task EraseAsync(long userId, CancellationToken cancellationToken = default)
    {
        await context.Set<Report>().Where(r => r.PatientId == userId).ExecuteDeleteAsync(cancellationToken);
        await context.Set<Assessment>().Where(a => a.UserId == userId).ExecuteDeleteAsync(cancellationToken);
        await context.Set<StressTrigger>().Where(t => t.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
