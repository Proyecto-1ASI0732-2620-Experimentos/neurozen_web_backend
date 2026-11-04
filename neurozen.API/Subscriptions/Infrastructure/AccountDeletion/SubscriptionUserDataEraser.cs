using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Application.Accounts;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Subscriptions.Domain.Model.Aggregates;

namespace neurozen.API.Subscriptions.Infrastructure.AccountDeletion;

/// <summary>Account deletion: removes the subscription (holder name, email and card hints).</summary>
public class SubscriptionUserDataEraser(AppDbContext context) : IUserDataEraser
{
    public int Order => 200;

    public async Task EraseAsync(long userId, CancellationToken cancellationToken = default) =>
        await context.Set<Subscription>().Where(s => s.UserId == userId).ExecuteDeleteAsync(cancellationToken);
}
