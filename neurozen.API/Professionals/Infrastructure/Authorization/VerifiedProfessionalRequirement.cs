using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.Security;

namespace neurozen.API.Professionals.Infrastructure.Authorization;

/// <summary>
///     Blocks PSYCHOLOGIST accounts whose professional profile is not VERIFIED (or was deleted).
///     With <see cref="RequirePsychologistRole" /> = true, only verified psychologists pass.
/// </summary>
public sealed class VerifiedProfessionalRequirement(bool requirePsychologistRole) : IForbiddenMessageRequirement
{
    public bool RequirePsychologistRole { get; } = requirePsychologistRole;

    public string ForbiddenMessage => RequirePsychologistRole
        ? "Solo los psicólogos verificados pueden realizar esta acción"
        : "Tu cuenta profesional aún no está verificada";
}

public sealed class VerifiedProfessionalHandler(IProfessionalRepository professionalRepository)
    : AuthorizationHandler<VerifiedProfessionalRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        VerifiedProfessionalRequirement requirement)
    {
        var role = context.User.FindFirst(HttpCurrentUser.RoleClaim)?.Value;
        var isPsychologist = string.Equals(role, AppRoles.Psychologist, StringComparison.OrdinalIgnoreCase);

        if (!isPsychologist)
        {
            if (!requirement.RequirePsychologistRole) context.Succeed(requirement);
            return;
        }

        if (!long.TryParse(context.User.FindFirst(HttpCurrentUser.SubjectClaim)?.Value, out var userId)) return;

        var professional = await professionalRepository.FindByUserIdAsync(userId);
        if (professional is { IsVisibleToPatients: true }) context.Succeed(requirement);
    }
}

public static class ProfessionalAuthorizationExtensions
{
    public static IServiceCollection AddProfessionalAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, VerifiedProfessionalHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(AppPolicies.VerifiedIfPsychologist, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .AddRequirements(new VerifiedProfessionalRequirement(false)))
            .AddPolicy(AppPolicies.VerifiedPsychologist, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireRole(AppRoles.Psychologist)
                .AddRequirements(new VerifiedProfessionalRequirement(true)));
        return services;
    }
}
