using Microsoft.EntityFrameworkCore;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using neurozen.API.Subscriptions.Domain.Model.Aggregates;
using neurozen.API.Subscriptions.Domain.Repositories;

namespace neurozen.API.Subscriptions.Infrastructure.Repositories;

public class SubscriptionRepository(AppDbContext context)
    : BaseRepository<Subscription>(context), ISubscriptionRepository
{
    public Task<Subscription?> FindByUserIdAsync(long userId) =>
        Context.Set<Subscription>().FirstOrDefaultAsync(s => s.UserId == userId);
}
