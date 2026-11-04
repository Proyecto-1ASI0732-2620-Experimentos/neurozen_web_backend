namespace neurozen.API.Subscriptions.Interfaces.REST.Resources;

/// <summary>Never contains the full card number nor the CVV.</summary>
public record SubscriptionResource(
    long Id,
    long UserId,
    int PlanId,
    bool IsActive,
    string NameUser,
    string LastNameUser,
    string EmailUser,
    string CardLast4,
    string ExpirationDate,
    DateTime CreatedAt);

/// <summary>Body sent by the frontend today. isActive is not bound (the server sets it).</summary>
public record CreateSubscriptionResource(
    long? UserId,
    int? PlanId,
    string? NameUser,
    string? LastNameUser,
    string? EmailUser,
    string? NumberCard,
    string? ExpirationDate,
    string? Cvv);
