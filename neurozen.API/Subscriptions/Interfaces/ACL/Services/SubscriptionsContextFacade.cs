using neurozen.API.Subscriptions.Domain.Repositories;

namespace neurozen.API.Subscriptions.Interfaces.ACL.Services;

public class SubscriptionsContextFacade(ISubscriptionRepository subscriptionRepository) : ISubscriptionsContextFacade
{
    public async Task<int?> GetActivePlanIdAsync(long userId)
    {
        var subscription = await subscriptionRepository.FindByUserIdAsync(userId);
        return subscription is { IsActive: true } ? subscription.PlanId : null;
    }
}
