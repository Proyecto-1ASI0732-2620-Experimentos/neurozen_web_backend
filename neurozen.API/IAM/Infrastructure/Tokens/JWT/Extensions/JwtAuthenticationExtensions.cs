using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.Security;

namespace neurozen.API.IAM.Infrastructure.Tokens.JWT.Extensions;

public static class JwtAuthenticationExtensions
{
    /// <summary>
    ///     JwtBearer validation (signature, issuer, audience, exp) plus a fallback policy that
    ///     requires an authenticated user on every endpoint without [AllowAnonymous].
    ///     401 only for a missing/invalid/expired token or a deleted account; 403 for a wrong role.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var settings = configuration.GetSection("TokenSettings").Get<TokenSettings>() ?? new TokenSettings();
        var signingKey = new SymmetricSecurityKey(settings.GetSigningKeyBytes());

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = HttpCurrentUser.SubjectClaim,
                    RoleClaimType = HttpCurrentUser.RoleClaim
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var subject = context.Principal?.FindFirst(HttpCurrentUser.SubjectClaim)?.Value;
                        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                        if (!long.TryParse(subject, out var userId) || !await users.ExistsByIdAsync(userId))
                            context.Fail("The account no longer exists");
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        if (context.Response.HasStarted) return;
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            new ErrorResponse("Tu sesión no es válida o venció. Inicia sesión de nuevo."));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(
                            new ErrorResponse("No tienes permiso para realizar esta acción"));
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
