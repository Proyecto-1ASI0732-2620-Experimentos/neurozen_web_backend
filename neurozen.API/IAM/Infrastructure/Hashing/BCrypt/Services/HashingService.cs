using neurozen.API.IAM.Application.Internal.OutboundServices;
using BCryptNet = BCrypt.Net.BCrypt;

namespace neurozen.API.IAM.Infrastructure.Hashing.BCrypt.Services;

/// <summary>BCrypt with an explicit work factor (cost ≥ 11 required by the specification).</summary>
public class HashingService : IHashingService
{
    public const int WorkFactor = 12;

    private static readonly Lazy<string> DummyHash =
        new(() => BCryptNet.HashPassword(Guid.NewGuid().ToString("N"), WorkFactor));

    public string HashPassword(string password) => BCryptNet.HashPassword(password, WorkFactor);

    public bool VerifyPassword(string password, string? passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            BCryptNet.Verify(password, DummyHash.Value);
            return false;
        }

        try
        {
            return BCryptNet.Verify(password, passwordHash);
        }
        catch (global::BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
