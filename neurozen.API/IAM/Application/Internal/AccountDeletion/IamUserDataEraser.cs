using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Shared.Application.Accounts;

namespace neurozen.API.IAM.Application.Internal.AccountDeletion;

/// <summary>Removes the user's password reset tokens. The user row itself is anonymized by the caller.</summary>
public class IamUserDataEraser(IPasswordResetTokenRepository tokenRepository) : IUserDataEraser
{
    public int Order => 900;

    public async Task EraseAsync(long userId, CancellationToken cancellationToken = default)
    {
        foreach (var token in await tokenRepository.ListByUserIdAsync(userId))
            tokenRepository.Remove(token);
    }
}
