using Microsoft.EntityFrameworkCore;
using dotnet_backend_2.DTOs;
using dotnet_backend_2.Data;
using dotnet_backend_2.Data.Entities;

namespace dotnet_backend_2.Services.Auth;

public class AuthService(ApplicationDbContext context, ITokenService tokenService) : IAuthService
{
    public async Task<bool> RegisterAsync(RegisterDto registerDto)
    {
        var userNameLower = registerDto.Username.ToLower();

        var existingUser = await context.Users
            .AnyAsync(u => u.Username.ToLower() == userNameLower);

        if (existingUser) return false;

        var newUser = new User
        {
            Username = registerDto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            Role = "User"

        };

        context.Users.Add(newUser);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<(LoginResponseDto? response, string? token)> LoginAsync(LoginDto loginDto)
    {
        var usernameLower = loginDto.Username.ToLower();

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == usernameLower);

        if (user is null) return (null, null);

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return (null, null);
        }

        var token = tokenService.CreateToken(user);

        var response = new LoginResponseDto(
            user.Username,
            user.Role,
            user.Id
        );

        return (response, token);
    }
}