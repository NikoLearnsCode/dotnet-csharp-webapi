using System.ComponentModel.DataAnnotations;

namespace WebApi.Services.Auth;

// Bound from the "Jwt" configuration section and validated at startup
// (ValidateOnStart in Program.cs), so a missing or short key fails fast
// instead of surfacing as a 401 at the first login.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    [
        Required,
        MinLength(64, ErrorMessage = "Jwt:Key must be at least 64 characters for HMAC-SHA512.")
    ]
    public string Key { get; set; } = "";

    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
}
