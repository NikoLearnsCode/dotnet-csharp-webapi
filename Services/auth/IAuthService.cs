using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Services.Auth;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterDto registerDto);
    Task<(LoginResponseDto? response, string? token)> LoginAsync(LoginDto loginDto);
}