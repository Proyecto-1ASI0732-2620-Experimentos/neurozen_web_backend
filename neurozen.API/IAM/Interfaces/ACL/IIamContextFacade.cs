using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.IAM.Interfaces.ACL;

/// <summary>Minimal user data other bounded contexts may read (no password hash, no profile details).</summary>
public record UserSummary(long Id, string FullName, string Email, bool IsDeleted)
{
    /// <summary>"Ana T." — for places where the full name must not be shown (reviews).</summary>
    public string AbbreviatedName => neurozen.API.IAM.Domain.Model.ValueObjects.PersonName.Abbreviate(FullName);
}

public interface IIamContextFacade
{
    Task<UserSummary?> FetchUserSummaryAsync(long userId);

    Task<IReadOnlyDictionary<long, UserSummary>> FetchUserSummariesAsync(IEnumerable<long> userIds);

    void ValidateFullName(string fullName, string field, ValidationErrors errors);

    /// <summary>Changes the user's full name. Does not save: the caller commits in its unit of work.</summary>
    Task ChangeFullNameAsync(long userId, string fullName);
}
