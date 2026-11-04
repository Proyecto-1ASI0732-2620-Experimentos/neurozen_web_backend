using neurozen.API.IAM.Domain.Model.Aggregates;

namespace neurozen.API.IAM.Application.Internal.OutboundServices;

public interface ITokenService
{
    /// <summary>Signed JWT with "sub" (user id), "role", "email" and "exp".</summary>
    string GenerateToken(User user);
}
