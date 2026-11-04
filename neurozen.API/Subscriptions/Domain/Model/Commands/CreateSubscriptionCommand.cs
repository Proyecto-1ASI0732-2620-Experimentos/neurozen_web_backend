namespace neurozen.API.Subscriptions.Domain.Model.Commands;

/// <summary>NumberCard and Cvv only live in memory during validation. isActive from the client is ignored.</summary>
public record CreateSubscriptionCommand(
    long? UserId,
    int? PlanId,
    string? NameUser,
    string? LastNameUser,
    string? EmailUser,
    string? NumberCard,
    string? ExpirationDate,
    string? Cvv);
