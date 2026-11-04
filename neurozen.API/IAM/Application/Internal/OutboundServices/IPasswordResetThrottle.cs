namespace neurozen.API.IAM.Application.Internal.OutboundServices;

/// <summary>Per-email limit for forgot-password requests (the per-IP limit is applied by ASP.NET rate limiting).</summary>
public interface IPasswordResetThrottle
{
    bool TryAcquire(string normalizedEmail);
}
