using System.Text;

namespace neurozen.API.IAM.Infrastructure.Tokens.JWT.Configuration;

/// <summary>
///     Bound from the "TokenSettings" section. The secret must come from an environment
///     variable (TokenSettings__Secret) or user-secrets, never from the repository.
/// </summary>
public class TokenSettings
{
    public const int MinimumSecretBytes = 32; // 256 bits for HS256

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "neurozen-api";
    public string Audience { get; set; } = "neurozen-web";
    public int ExpirationMinutes { get; set; } = 120;

    /// <summary>Signing key bytes; throws at startup if the secret is missing or shorter than 256 bits.</summary>
    public byte[] GetSigningKeyBytes()
    {
        var bytes = Encoding.UTF8.GetBytes(Secret ?? string.Empty);
        if (bytes.Length < MinimumSecretBytes)
            throw new InvalidOperationException(
                $"TokenSettings:Secret must be at least {MinimumSecretBytes} bytes (256 bits). " +
                "Set it with the TokenSettings__Secret environment variable or dotnet user-secrets.");
        return bytes;
    }
}
