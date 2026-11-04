using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Security;

namespace neurozen.API.Stress.Application.Internal;

/// <summary>Ownership rules for the USER-only health data (triggers, assessments, own reports).</summary>
public static class StressAccess
{
    private const string ForbiddenMessage = "Solo puedes acceder a tus propios registros";

    /// <summary>For ?userId= / ?patientId= reads: required, the caller must be that USER.</summary>
    public static long RequireOwnUserId(ICurrentUser currentUser, long? requestedUserId, string parameterName)
    {
        if (requestedUserId is null)
        {
            var message = $"{parameterName} es obligatorio";
            throw new ValidationFailedException(message,
                new Dictionary<string, string[]> { [parameterName] = [message] });
        }

        if (!currentUser.IsInRole(AppRoles.User) || requestedUserId.Value != currentUser.Id)
            throw new ForbiddenOperationException(ForbiddenMessage);

        return requestedUserId.Value;
    }

    /// <summary>For creation: the owner is always the token's user; a different id in the body is a 403.</summary>
    public static long ResolveOwnUserId(ICurrentUser currentUser, long? requestedUserId)
    {
        if (requestedUserId is not null && requestedUserId.Value != currentUser.Id)
            throw new ForbiddenOperationException(ForbiddenMessage);
        return currentUser.Id;
    }
}
