using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Wellness.Application.Internal;

internal static class WellnessAccess
{
    public static long RequireOwnUserId(ICurrentUser currentUser, long userId)
    {
        if (userId != currentUser.Id)
            throw new ForbiddenOperationException("No tienes permiso para acceder a estos datos");

        return userId;
    }
}
