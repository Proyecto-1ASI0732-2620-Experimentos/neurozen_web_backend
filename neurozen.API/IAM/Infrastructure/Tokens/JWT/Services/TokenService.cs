using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Configuration;

namespace neurozen.API.IAM.Infrastructure.Tokens.JWT.Services;

/// <summary>
///     Issues HS256 JWTs with "sub", "role", "email", "jti", "iss", "aud" and "exp" (seconds).
///     Validation is done by the JwtBearer handler configured in Program.cs with the same settings.
/// </summary>
public class TokenService(IOptions<TokenSettings> tokenSettings) : ITokenService
{
    private readonly TokenSettings _settings = tokenSettings.Value;

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(_settings.GetSigningKeyBytes());
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("role", user.Role.ToWire()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_settings.ExpirationMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
