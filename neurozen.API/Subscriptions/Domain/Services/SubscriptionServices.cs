using neurozen.API.Subscriptions.Domain.Model.Aggregates;
using neurozen.API.Subscriptions.Domain.Model.Commands;
using neurozen.API.Subscriptions.Domain.Model.Queries;

namespace neurozen.API.Subscriptions.Domain.Services;

/// <summary>Created = false when an existing subscription of the user was replaced (plan change).</summary>
public record SubscriptionResult(Subscription Subscription, bool Created);

public interface ISubscriptionCommandService
{
    Task<SubscriptionResult> Handle(CreateSubscriptionCommand command);
}

public interface ISubscriptionQueryService
{
    /// <summary>Owner only (403). 404 when the user has no subscription.</summary>
    Task<Subscription> Handle(GetSubscriptionByUserIdQuery query);
}
