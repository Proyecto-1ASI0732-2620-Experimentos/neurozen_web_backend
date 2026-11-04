using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Subscriptions.Domain.Model.Aggregates;

namespace neurozen.API.Subscriptions.Domain.Repositories;

public interface ISubscriptionRepository : IBaseRepository<Subscription>
{
    Task<Subscription?> FindByUserIdAsync(long userId);
}
