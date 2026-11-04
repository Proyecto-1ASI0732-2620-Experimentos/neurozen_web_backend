namespace neurozen.API.IAM.Application.Internal.OutboundServices;

public interface IHashingService
{
    string HashPassword(string password);

    /// <summary>
    ///     Verifies a password. When <paramref name="passwordHash" /> is null it still spends the same
    ///     time hashing (against a dummy hash) and returns false, so response times do not reveal
    ///     whether an account exists.
    /// </summary>
    bool VerifyPassword(string password, string? passwordHash);
}
