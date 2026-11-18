using neurozen.API.IAM.Domain.Model.Entities;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.IAM.Domain.Repositories;

public interface IUserConsentRepository : IBaseRepository<UserConsent>
{
    Task<IReadOnlyList<UserConsent>> ListByUserIdAsync(long userId);
}
