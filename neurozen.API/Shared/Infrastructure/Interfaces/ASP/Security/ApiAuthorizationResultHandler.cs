using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.Security;

/// <summary>A requirement that explains why access was denied (the message is sent with the 403).</summary>
public interface IForbiddenMessageRequirement : IAuthorizationRequirement
{
    string ForbiddenMessage { get; }
}

/// <summary>
///     Writes { "message": ... } for 403 responses caused by a requirement that carries its own message.
///     Everything else (401 challenge, plain role 403) goes through the default handler and the JwtBearer events.
/// </summary>
public sealed class ApiAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var requirement = authorizeResult.Forbidden
            ? authorizeResult.AuthorizationFailure?.FailedRequirements.OfType<IForbiddenMessageRequirement>()
                .FirstOrDefault()
            : null;

        if (requirement is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ErrorResponse(requirement.ForbiddenMessage));
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
