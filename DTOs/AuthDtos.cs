using System.ComponentModel.DataAnnotations;

namespace dotnet_backend_2.DTOs;

public record RegisterDto
{
    [Required(ErrorMessage = "Username is required.")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
    [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters.")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number.")]
    public required string Password { get; init; }
}

public record LoginDto
{
    [Required(ErrorMessage = "Please provide a username.")]
    public required string Username { get; init; }
    [Required(ErrorMessage = "Please provide a password.")]
    public required string Password { get; init; }
}

public record LoginResponseDto(
    string Username,
    string Role,
    int UserId
);

