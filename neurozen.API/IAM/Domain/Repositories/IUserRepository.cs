using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.IAM.Domain.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>Finds a user by email (expects an already normalized email).</summary>
    Task<User?> FindByEmailAsync(string email);

    Task<bool> ExistsByEmailAsync(string email);

    Task<IReadOnlyList<User>> ListByIdsAsync(IReadOnlyCollection<long> ids);

    Task<bool> ExistsByEmailForOtherUserAsync(string email, long excludedUserId);

    /// <summary>True only for active (not deleted) accounts. Used to reject tokens of deleted accounts.</summary>
    Task<bool> ExistsByIdAsync(long id);

    /// <summary>Used during JWT validation to verify the current security stamp without loading the full user.</summary>
    Task<(string SecurityStamp, bool IsDeleted)?> GetSecurityStampAndDeletedStatusAsync(long id);
}
