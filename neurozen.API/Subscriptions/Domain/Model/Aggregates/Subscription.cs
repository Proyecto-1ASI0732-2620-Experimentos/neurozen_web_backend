namespace neurozen.API.Subscriptions.Domain.Model.Aggregates;

/// <summary>
///     Plan subscription of a user (one per user). Stores only the last 4 digits and the expiry of the card;
///     never the full number nor the CVV.
/// </summary>
public class Subscription
{
    public const int MinPlanId = 1;
    public const int MaxPlanId = 3;
    public const int NameMaxLength = 60;
    public const int EmailMaxLength = 254;

    protected Subscription()
    {
        HolderFirstName = string.Empty;
        HolderLastName = string.Empty;
        Email = string.Empty;
        CardLast4 = string.Empty;
        CardExpiry = string.Empty;
    }

    public Subscription(long userId, int planId, string holderFirstName, string holderLastName, string email,
        string cardLast4, string cardExpiry)
    {
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        HolderFirstName = holderFirstName;
        HolderLastName = holderLastName;
        Email = email;
        CardLast4 = cardLast4;
        CardExpiry = cardExpiry;
        Apply(planId, holderFirstName, holderLastName, email, cardLast4, cardExpiry);
    }

    public long Id { get; private set; }
    public long UserId { get; private set; }
    public int PlanId { get; private set; }
    public string HolderFirstName { get; private set; }
    public string HolderLastName { get; private set; }
    public string Email { get; private set; }
    public string CardLast4 { get; private set; }

    /// <summary>"MM/YY".</summary>
    public string CardExpiry { get; private set; }

    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    ///     Sets the plan and card data. There is no payment gateway yet, so a subscription whose data
    ///     passes validation is considered paid and active.
    /// </summary>
    public void Apply(int planId, string holderFirstName, string holderLastName, string email, string cardLast4,
        string cardExpiry)
    {
        PlanId = planId;
        HolderFirstName = holderFirstName;
        HolderLastName = holderLastName;
        Email = email;
        CardLast4 = cardLast4;
        CardExpiry = cardExpiry;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
