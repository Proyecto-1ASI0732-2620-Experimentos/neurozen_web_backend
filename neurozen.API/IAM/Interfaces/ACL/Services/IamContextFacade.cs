using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.IAM.Interfaces.ACL.Services;

public class IamContextFacade(IUserRepository userRepository) : IIamContextFacade
{
    public async Task<UserSummary?> FetchUserSummaryAsync(long userId)
    {
        var user = await userRepository.FindByIdAsync(userId);
        return user is null ? null : new UserSummary(user.Id, user.FullName, user.Email, user.IsDeleted);
    }

    public async Task<IReadOnlyDictionary<long, UserSummary>> FetchUserSummariesAsync(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, UserSummary>();

        var users = await userRepository.ListByIdsAsync(ids);
        return users.ToDictionary(
            user => user.Id,
            user => new UserSummary(user.Id, user.FullName, user.Email, user.IsDeleted));
    }

    public void ValidateFullName(string fullName, string field, ValidationErrors errors) =>
        PersonName.Validate(fullName, field, errors);

    public async Task ChangeFullNameAsync(long userId, string fullName)
    {
        var user = await userRepository.FindByIdAsync(userId)
                   ?? throw new ResourceNotFoundException("La cuenta no existe");
        user.ChangeFullName(fullName);
    }
}
