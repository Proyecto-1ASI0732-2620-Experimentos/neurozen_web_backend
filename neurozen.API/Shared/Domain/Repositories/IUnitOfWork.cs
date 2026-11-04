namespace neurozen.API.Shared.Domain.Repositories;

/// <summary>
///     Unit of work interface
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Commit pending changes to the database.</summary>
    Task CompleteAsync();

    /// <summary>
    ///     Runs <paramref name="operation" /> inside a database transaction (commit on success,
    ///     rollback on any exception). Nested calls reuse the outer transaction.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
