
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Configuration;

namespace neurozen.API.IAM.Infrastructure.Tokens.JWT.Services;

/**
 * <summary>
 *     The token service
 * </summary>
 * <remarks>
 *     This class is used to generate and validate tokens
 * </remarks>
 */
public class TokenService(IOptions<TokenSettings> tokenSettings) : ITokenService
{
    private readonly TokenSettings _tokenSettings = tokenSettings.Value;

    /**
     * <summary>
     *     Generate token
     * </summary>
     * <param name="user">The user for token generation</param>
     * <returns>The generated Token</returns>
     */
    public string GenerateToken(User user)
    {
        var secret = _tokenSettings.Secret ?? string.Empty;
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("TokenSettings:Secret must be set to generate tokens.");

        // Derive a 256-bit key from the configured secret using SHA-256 so
        // callers may provide passphrases of any length while satisfying
        // algorithm minimum key size requirements.
        byte[] key;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            key = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };
        var tokenHandler = new JsonWebTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return token;
    }

    /**
     * <summary>
     *     VerifyPassword token
     * </summary>
     * <param name="token">The token to validate</param>
     * <returns>The user id if the token is valid, null otherwise</returns>
     */
    public async Task<Guid?> ValidateToken(string token)
    {
        // If token is null or empty
        if (string.IsNullOrEmpty(token))
            // Return null 
            return null;
        // Otherwise, perform validation
        var secret = _tokenSettings.Secret ?? string.Empty;
        if (string.IsNullOrWhiteSpace(secret))
            return null;

        byte[] key;
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            key = sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }
        var tokenHandler = new JsonWebTokenHandler();
        try
        {
            var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                // Expiration without delay
                ClockSkew = TimeSpan.Zero
            });

            var jwtToken = (JsonWebToken)tokenValidationResult.SecurityToken;
            var userId = Guid.Parse(jwtToken.Claims.First(claim => claim.Type == ClaimTypes.Sid).Value);
            return userId;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}