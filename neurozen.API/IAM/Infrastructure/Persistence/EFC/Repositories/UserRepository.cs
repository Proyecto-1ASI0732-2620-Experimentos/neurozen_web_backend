using Microsoft.EntityFrameworkCore;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

namespace neurozen.API.IAM.Infrastructure.Persistence.EFC.Repositories;

public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    public Task<User?> FindByEmailAsync(string email) =>
        Context.Set<User>().FirstOrDefaultAsync(u => u.Email == email);

    public async Task<IReadOnlyList<User>> ListByIdsAsync(IReadOnlyCollection<long> ids) =>
        await Context.Set<User>().Where(u => ids.Contains(u.Id)).ToListAsync();

    public Task<bool> ExistsByEmailAsync(string email) =>
        Context.Set<User>().AnyAsync(u => u.Email == email);

    public Task<bool> ExistsByEmailForOtherUserAsync(string email, long excludedUserId) =>
        Context.Set<User>().AnyAsync(u => u.Email == email && u.Id != excludedUserId);

    public Task<bool> ExistsByIdAsync(long id) =>
        Context.Set<User>().AnyAsync(u => u.Id == id && u.DeletedAt == null);
}
