using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Subscriptions.Domain.Model.Aggregates;
using neurozen.API.Subscriptions.Domain.Model.Queries;
using neurozen.API.Subscriptions.Domain.Repositories;
using neurozen.API.Subscriptions.Domain.Services;

namespace neurozen.API.Subscriptions.Application.Internal.QueryServices;

public class SubscriptionQueryService(ISubscriptionRepository subscriptionRepository, ICurrentUser currentUser)
    : ISubscriptionQueryService
{
    public async Task<Subscription> Handle(GetSubscriptionByUserIdQuery query)
    {
        if (query.UserId != currentUser.Id)
            throw new ForbiddenOperationException("Solo puedes ver tu propia suscripción");

        // 404 means "no plan" for the frontend, not an error.
        return await subscriptionRepository.FindByUserIdAsync(query.UserId)
               ?? throw new ResourceNotFoundException("No tienes una suscripción");
    }
}
