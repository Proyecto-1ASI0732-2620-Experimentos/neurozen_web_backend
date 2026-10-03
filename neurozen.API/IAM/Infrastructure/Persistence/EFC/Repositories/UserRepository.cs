using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Repositories;

namespace neurozen.API.IAM.Infrastructure.Persistence.EFC.Repositories;

/**
 * <summary>
 *     The user repository
 * </summary>
 * <remarks>
 *     This repository is used to manage users
 * </remarks>
 */
public class UserRepository(AppDbContext context) : IUserRepository
{
    private readonly AppDbContext Context = context;

    /**
     * <summary>
     *     Add a user
     * </summary>
     * <param name="entity">The user to add</param>
     */
    public async Task AddAsync(User entity)
    {
        var userEntity = new UserManagement.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            Email = entity.Username,
            PasswordHash = entity.PasswordHash,
            FullName = entity.Username,
            Role = "user",
            AvatarUrl = null,
            Meta = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await Context.Set<UserManagement.Domain.Entities.User>().AddAsync(userEntity);
    }

    /**
     * <summary>
     *     Find a user by id
     * </summary>
     * <param name="id">The user id</param>
     * <returns>The user</returns>
     */
    public Task<User?> FindByIdAsync(int id)
    {
        return Task.FromResult<User?>(null);
    }

    public async Task<User?> FindByIdAsync(Guid id)
    {
        var userEntity = await Context.Set<UserManagement.Domain.Entities.User>().FindAsync(id);
        if (userEntity == null) return null;
        var user = new User(userEntity.Email, userEntity.PasswordHash ?? string.Empty);
        user.SetId(userEntity.Id);
        return user;
    }

    /**
     * <summary>
     *     Update a user
     * </summary>
     * <param name="entity">The user to update</param>
     */
    public void Update(User entity)
    {
        var userEntity = Context.Set<UserManagement.Domain.Entities.User>()
            .FirstOrDefault(u => u.Email.Equals(entity.Username));

        if (userEntity != null)
        {
            userEntity.PasswordHash = entity.PasswordHash;
            Context.Set<UserManagement.Domain.Entities.User>().Update(userEntity);
        }
    }

    /**
     * <summary>
     *     Remove a user
     * </summary>
     * <param name="entity">The user to remove</param>
     */
    public void Remove(User entity)
    {
        var userEntity = Context.Set<UserManagement.Domain.Entities.User>()
            .FirstOrDefault(u => u.Email.Equals(entity.Username));

        if (userEntity != null)
        {
            Context.Set<UserManagement.Domain.Entities.User>().Remove(userEntity);
        }
    }

    /**
     * <summary>
     *     List all users
     * </summary>
     * <returns>List of users</returns>
     */
    public async Task<IEnumerable<User>> ListAsync()
    {
        var userEntities = await Context.Set<UserManagement.Domain.Entities.User>().ToListAsync();
        return userEntities.Select(u =>
        {
            var user = new User(u.Email, u.PasswordHash ?? string.Empty);
            user.SetId(u.Id);
            return user;
        }).ToList();
    }

    /**
     * <summary>
     *     Find a user by username
     * </summary>
     * <param name="username">The username to search</param>
     * <returns>The user</returns>
     */
    public async Task<User?> FindByUsernameAsync(string username)
    {
        var userEntity = await Context.Set<UserManagement.Domain.Entities.User>()
            .FirstOrDefaultAsync(user => user.Email.Equals(username));

        if (userEntity == null) return null;

        var user = new User(userEntity.Email, userEntity.PasswordHash ?? string.Empty);
        user.SetId(userEntity.Id);
        return user;
    }

    /**
     * <summary>
     *     Check if a user exists by username
     * </summary>
     * <param name="username">The username to search</param>
     * <returns>True if the user exists, false otherwise</returns>
     */
    public bool ExistsByUsername(string username)
    {
        return Context.Set<UserManagement.Domain.Entities.User>().Any(user => user.Email.Equals(username));
    }
}