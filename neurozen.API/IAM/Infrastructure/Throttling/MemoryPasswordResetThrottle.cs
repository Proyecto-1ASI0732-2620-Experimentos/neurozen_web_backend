using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using neurozen.API.IAM.Application.Internal.Configuration;
using neurozen.API.IAM.Application.Internal.OutboundServices;

namespace neurozen.API.IAM.Infrastructure.Throttling;

/// <summary>
///     Fixed-window counter per email kept in memory. Good for a single instance;
///     with several instances it should move to a shared store (e.g. Redis).
/// </summary>
public sealed class MemoryPasswordResetThrottle(IMemoryCache cache, IOptions<PasswordResetSettings> settings)
    : IPasswordResetThrottle
{
    private sealed class Counter
    {
        public int Count;
    }

    public bool TryAcquire(string normalizedEmail)
    {
        var window = TimeSpan.FromMinutes(Math.Max(1, settings.Value.EmailWindowMinutes));
        var counter = cache.GetOrCreate($"pwd-reset:{normalizedEmail}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = window;
            return new Counter();
        })!;

        return Interlocked.Increment(ref counter.Count) <= settings.Value.MaxRequestsPerEmail;
    }
}
