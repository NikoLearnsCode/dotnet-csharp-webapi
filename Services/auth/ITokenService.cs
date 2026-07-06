using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WebApi.Data.Entities;

namespace WebApi.Services.Auth;

public interface ITokenService
{
    string CreateAccessToken(User user);

    /// <summary>Cryptographically random opaque refresh token (base64url).</summary>
    string CreateRefreshTokenValue();

    /// <summary>SHA-256 hash used for storage and lookup; the raw value never touches the database.</summary>
    string HashToken(string rawToken);
}

public class TokenService(IOptions<JwtOptions> jwtOptions, TimeProvider timeProvider)
    : ITokenService
{
    public string CreateAccessToken(User user)
    {
        var options = jwtOptions.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role),
                    // Unique token id; also guarantees two tokens minted within the
                    // same second (login immediately followed by refresh) differ.
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                ]
            ),
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now + options.AccessTokenLifetime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public string CreateRefreshTokenValue() =>
        Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    public string HashToken(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
