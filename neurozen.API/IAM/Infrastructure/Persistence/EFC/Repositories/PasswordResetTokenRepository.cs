using Microsoft.EntityFrameworkCore;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace neurozen.API.IAM.Infrastructure.Persistence.EFC.Repositories;

public class PasswordResetTokenRepository(AppDbContext context)
    : BaseRepository<PasswordResetToken>(context), IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash) =>
        Context.Set<PasswordResetToken>().FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

    public async Task<IReadOnlyList<PasswordResetToken>> ListUnusedByUserIdAsync(long userId) =>
        await Context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId && t.UsedAt == null)
            .ToListAsync();

    public async Task<IReadOnlyList<PasswordResetToken>> ListByUserIdAsync(long userId) =>
        await Context.Set<PasswordResetToken>()
            .Where(t => t.UserId == userId)
            .ToListAsync();
}
