using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.RateLimiting;

/// <summary>
///     Per-IP limits for the public authentication endpoints (fixed windows, no queue).
///     Limits are configurable under "RateLimiting" (AuthPermitLimit, AuthWindowSeconds, ...).
/// </summary>
public static class RateLimitPolicies
{
    public const string Authentication = "authentication";
    public const string PasswordReset = "password-reset";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting");
        var authLimit = section.GetValue("AuthPermitLimit", 10);
        var authWindow = TimeSpan.FromSeconds(section.GetValue("AuthWindowSeconds", 60));
        var resetLimit = section.GetValue("PasswordResetPermitLimit", 5);
        var resetWindow = TimeSpan.FromSeconds(section.GetValue("PasswordResetWindowSeconds", 900));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ErrorResponse("Demasiados intentos. Espera unos minutos y vuelve a intentarlo."),
                    cancellationToken);
            };

            options.AddPolicy(Authentication, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                ClientIp(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authLimit, Window = authWindow, QueueLimit = 0
                }));

            options.AddPolicy(PasswordReset, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                ClientIp(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = resetLimit, Window = resetWindow, QueueLimit = 0
                }));
        });

        return services;
    }

    private static string ClientIp(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var first = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
