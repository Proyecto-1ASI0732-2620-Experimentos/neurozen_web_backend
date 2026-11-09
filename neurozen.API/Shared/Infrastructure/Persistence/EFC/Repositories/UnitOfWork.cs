using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;

/// <summary>
///     Unit of work implementation over the single AppDbContext.
/// </summary>
public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CompleteAsync()
    {
        await context.SaveChangesAsync();
    }

    /// <summary>
    ///     Wraps the callback in an execution strategy and a database transaction. The callback must be
    ///     re-runnable in the face of transient failures: fresh tracked entities should be loaded inside the
    ///     callback, and external side effects must be guarded so they are not repeated unless safe.
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            await operation();
            return;
        }

        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(
            state: context,
            operation: async (dbContext, _, token) =>
            {
                dbContext.ChangeTracker.Clear();
                await using var transaction = await dbContext.Database.BeginTransactionAsync(token);
                try
                {
                    await operation();
                    await transaction.CommitAsync(token);
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken);
    }
}
