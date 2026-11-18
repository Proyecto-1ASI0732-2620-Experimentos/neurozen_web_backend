using Microsoft.EntityFrameworkCore;
using neurozen.API.IAM.Domain.Model.Entities;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace neurozen.API.IAM.Infrastructure.Persistence.EFC.Repositories;

public class UserConsentRepository(AppDbContext context)
    : BaseRepository<UserConsent>(context), IUserConsentRepository
{
    public async Task<IReadOnlyList<UserConsent>> ListByUserIdAsync(long userId) =>
        await Context.Set<UserConsent>()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.AcceptedAt)
            .ToListAsync();
}
