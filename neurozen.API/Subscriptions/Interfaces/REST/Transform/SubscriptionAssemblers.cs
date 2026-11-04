using neurozen.API.Subscriptions.Domain.Model.Aggregates;
using neurozen.API.Subscriptions.Domain.Model.Commands;
using neurozen.API.Subscriptions.Interfaces.REST.Resources;

namespace neurozen.API.Subscriptions.Interfaces.REST.Transform;

public static class SubscriptionAssembler
{
    public static SubscriptionResource ToResource(Subscription s) => new(
        s.Id, s.UserId, s.PlanId, s.IsActive, s.HolderFirstName, s.HolderLastName, s.Email, s.CardLast4,
        s.CardExpiry, s.CreatedAt);

    public static CreateSubscriptionCommand ToCommand(CreateSubscriptionResource r) =>
        new(r.UserId, r.PlanId, r.NameUser, r.LastNameUser, r.EmailUser, r.NumberCard, r.ExpirationDate, r.Cvv);
}
