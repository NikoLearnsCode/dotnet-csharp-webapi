using WebApi.DTOs;

namespace WebApi.Services.Auth;

/// <summary>Raw token pair handed to the client as cookies; only the refresh token's hash is persisted.</summary>
public record AuthTokens(string AccessToken, string RefreshToken);

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterDto registerDto);
    Task<(LoginResponseDto? response, AuthTokens? tokens)> LoginAsync(LoginDto loginDto);

    /// <summary>Rotates the refresh token. Returns null when it is unknown, expired, or replayed.</summary>
    Task<AuthTokens?> RefreshAsync(string rawRefreshToken);

    Task LogoutAsync(string? rawRefreshToken);
}
