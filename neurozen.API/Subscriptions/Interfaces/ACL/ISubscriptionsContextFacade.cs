namespace neurozen.API.Subscriptions.Interfaces.ACL;

/// <summary>Entry point used by other bounded contexts (Resource library) to read a user's plan.</summary>
public interface ISubscriptionsContextFacade
{
    /// <summary>Plan id (1 = Básico, 2 = Avanzado, 3 = Profesional) of the user's ACTIVE subscription; null if none.</summary>
    Task<int?> GetActivePlanIdAsync(long userId);
}
