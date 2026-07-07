using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApi.Data;
using WebApi.Data.Entities;
using WebApi.DTOs;

namespace WebApi.Services.Auth;

public class AuthService(
    ApplicationDbContext context,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider
) : IAuthService
{
    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        var userNameLower = registerDto.Username.ToLower();

        var existingUser = await context.Users.AnyAsync(u => u.Username == userNameLower);

        if (existingUser)
            return false;

        var newUser = new User
        {
            Username = registerDto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            Role = "User",
        };

        context.Users.Add(newUser);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Concurrent registration of the same username; the unique index wins.
            return false;
        }
        return true;
    }

    public async Task<(LoginResponseDto? response, AuthTokens? tokens)> LoginAsync(
        LoginDto loginDto
    )
    {
        var usernameLower = loginDto.Username.ToLower();

        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Username.ToLower() == usernameLower
        );

        if (user is null)
            return (null, null);

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return (null, null);
        }

        var tokens = await IssueTokensAsync(user, replacing: null);

        var response = new LoginResponseDto(user.Username, user.Role, user.Id);

        return (response, tokens);
    }

    public async Task<AuthTokens?> RefreshAsync(string rawRefreshToken)
    {
        var hash = tokenService.HashToken(rawRefreshToken);
        var stored = await context
            .RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (stored is null)
            return null;

        var now = timeProvider.GetUtcNow().UtcDateTime;

        if (stored.RevokedAtUtc is not null)
        {
            // A rotated token came back: the raw value leaked (replay).
            // Revoke every active token for the user to end the whole session family.
            await context
                .RefreshTokens.Where(rt => rt.UserId == stored.UserId && rt.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.RevokedAtUtc, now));
            return null;
        }

        if (stored.ExpiresAtUtc <= now)
            return null;

        // User comes fresh from the database, so role changes take effect here.
        return await IssueTokensAsync(stored.User!, replacing: stored);
    }

    public async Task LogoutAsync(string? rawRefreshToken)
    {
        if (string.IsNullOrEmpty(rawRefreshToken))
            return;

        var hash = tokenService.HashToken(rawRefreshToken);
        var stored = await context.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.TokenHash == hash && rt.RevokedAtUtc == null
        );

        if (stored is null)
            return;

        stored.RevokedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync();
    }

    private async Task<AuthTokens> IssueTokensAsync(User user, RefreshToken? replacing)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var rawRefreshToken = tokenService.CreateRefreshTokenValue();
        var refreshTokenHash = tokenService.HashToken(rawRefreshToken);

        // Prune the user's expired rows. Revoked-but-unexpired rows must stay -
        // they are what makes replay detection work - but an expired token is
        // rejected by the lifetime check regardless, so its row is dead weight.
        await context
            .RefreshTokens.Where(rt => rt.UserId == user.Id && rt.ExpiresAtUtc <= now)
            .ExecuteDeleteAsync();

        if (replacing is not null)
        {
            replacing.RevokedAtUtc = now;
            replacing.ReplacedByTokenHash = refreshTokenHash;
        }

        context.RefreshTokens.Add(
            new RefreshToken
            {
                TokenHash = refreshTokenHash,
                UserId = user.Id,
                CreatedAtUtc = now,
                ExpiresAtUtc = now + jwtOptions.Value.RefreshTokenLifetime,
            }
        );
        await context.SaveChangesAsync();

        return new AuthTokens(tokenService.CreateAccessToken(user), rawRefreshToken);
    }
}
