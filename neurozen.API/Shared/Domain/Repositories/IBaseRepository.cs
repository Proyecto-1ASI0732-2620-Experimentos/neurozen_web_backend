namespace neurozen.API.Shared.Domain.Repositories;

/// <summary>
///     Base repository interface for all repositories. Every aggregate uses a numeric (long) id.
/// </summary>
/// <typeparam name="TEntity">The Entity Type</typeparam>
public interface IBaseRepository<TEntity>
{
    Task AddAsync(TEntity entity);

    Task<TEntity?> FindByIdAsync(long id);

    void Update(TEntity entity);

    void Remove(TEntity entity);

    Task<IEnumerable<TEntity>> ListAsync();
}
