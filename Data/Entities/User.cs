using Microsoft.EntityFrameworkCore;

namespace WebApi.Data.Entities;

[Index(nameof(Username), IsUnique = true)]
public class User
{
    public int Id { get; set; }
    public required string Username { get; init; }
    public required string PasswordHash { get; set; }

    // Mutable: role changes take effect on the next token refresh.
    public required string Role { get; set; }
}
