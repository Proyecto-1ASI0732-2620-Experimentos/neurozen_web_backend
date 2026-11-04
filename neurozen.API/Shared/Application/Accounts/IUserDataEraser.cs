namespace neurozen.API.Shared.Application.Accounts;

/// <summary>
///     Account deletion participant (Ley N.° 29733, derecho de cancelación).
///     Each bounded context registers one implementation that deletes or anonymizes its own data
///     for the user. IAM calls all of them inside a single transaction and then anonymizes the
///     user row. Implementations only modify the change tracker or run ExecuteDelete/ExecuteUpdate;
///     they never commit on their own.
/// </summary>
public interface IUserDataEraser
{
    /// <summary>Lower values run first.</summary>
    int Order { get; }

    Task EraseAsync(long userId, CancellationToken cancellationToken = default);
}
