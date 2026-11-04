using neurozen.API.Shared.Application.Security;

namespace neurozen.API.Shared.Infrastructure.Interfaces.ASP.Security;

/// <summary>
///     Reads the caller from HttpContext.User. JwtBearer runs with MapInboundClaims = false,
///     so the claims keep their JWT names ("sub", "role").
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public const string SubjectClaim = "sub";
    public const string RoleClaim = "role";

    private System.Security.Claims.ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public long Id
    {
        get
        {
            var value = Principal?.FindFirst(SubjectClaim)?.Value;
            if (!IsAuthenticated || !long.TryParse(value, out var id))
                throw new InvalidOperationException("The current request has no authenticated user.");
            return id;
        }
    }

    public string Role => Principal?.FindFirst(RoleClaim)?.Value ?? string.Empty;

    public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
}
