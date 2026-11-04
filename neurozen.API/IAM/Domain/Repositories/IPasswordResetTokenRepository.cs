using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.IAM.Domain.Repositories;

public interface IPasswordResetTokenRepository : IBaseRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash);

    /// <summary>Tokens of the user that are not used yet (they are invalidated when a new one is issued).</summary>
    Task<IReadOnlyList<PasswordResetToken>> ListUnusedByUserIdAsync(long userId);

    Task<IReadOnlyList<PasswordResetToken>> ListByUserIdAsync(long userId);
}
