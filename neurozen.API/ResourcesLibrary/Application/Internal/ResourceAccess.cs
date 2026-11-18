using neurozen.API.ResourcesLibrary.Domain.Model.Aggregates;
using neurozen.API.ResourcesLibrary.Domain.Model.ValueObjects;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Security;
using neurozen.API.Subscriptions.Interfaces.ACL;

namespace neurozen.API.ResourcesLibrary.Application.Internal;

/// <summary>Resolves which resource plan the caller can see (see <see cref="ResourcePlan" />).</summary>
public class ResourceAccess(ICurrentUser currentUser, ISubscriptionsContextFacade subscriptionsContextFacade)
{
    public async Task<ResourcePlan> GetCallerPlanAsync()
    {
        // Admins manage content; psychologists reach this point only when VERIFIED (VerifiedIfPsychologist policy).
        if (currentUser.IsInRole(AppRoles.Admin) || currentUser.IsInRole(AppRoles.Psychologist))
            return ResourcePlan.Professional;

        var planId = await subscriptionsContextFacade.GetActivePlanIdAsync(currentUser.Id);
        return planId is { } id && ResourcePlanExtensions.IsValid(id) ? (ResourcePlan)id : ResourcePlan.Basic;
    }

    public static bool CanSee(ResourcePlan callerPlan, ResourceLibrary resource) =>
        resource.RequiredPlan <= callerPlan;

    public static ForbiddenOperationException PlanRequired(ResourceLibrary resource) =>
        new($"Este recurso está disponible desde el plan {resource.RequiredPlan.DisplayName()}");
}
