namespace neurozen.API.Shared.Application.Security;

/// <summary>
///     The authenticated caller, read from the validated JWT.
///     Every ownership check (BOLA) compares resource owners against <see cref="Id" />.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The "sub" claim. Throws if the request is not authenticated.</summary>
    long Id { get; }

    /// <summary>The "role" claim (USER, PSYCHOLOGIST or ADMIN).</summary>
    string Role { get; }

    bool IsInRole(string role);
}
