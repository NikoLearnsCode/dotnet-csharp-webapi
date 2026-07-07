using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebApi.Tests.Infrastructure;

/// <summary>
/// Test authentication that replaces JWT in integration tests. Each request picks
/// its own identity via headers, so a single client can act as admin, a specific
/// user, or anonymous:
/// <list type="bullet">
///   <item><c>X-Test-Role</c>   → <see cref="ClaimTypes.Role"/> (e.g. "Admin", "User")</item>
///   <item><c>X-Test-UserId</c> → <see cref="ClaimTypes.NameIdentifier"/> (defaults to "1")</item>
///   <item><c>X-Test-User</c>   → <see cref="ClaimTypes.Name"/> (defaults to "testuser")</item>
/// </list>
/// When <c>X-Test-Role</c> is absent the request stays anonymous, so <c>[Authorize]</c>
/// endpoints return 401 - letting tests cover both authorized and unauthorized paths.
/// </summary>
public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";
    public const string UserIdHeader = "X-Test-UserId";
    public const string UserNameHeader = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = Request.Headers.TryGetValue(UserIdHeader, out var id) ? id.ToString() : "1";
        var userName = Request.Headers.TryGetValue(UserNameHeader, out var name)
            ? name.ToString()
            : "testuser";

        Claim[] claims =
        [
            new(ClaimTypes.Name, userName),
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role.ToString()),
        ];

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
