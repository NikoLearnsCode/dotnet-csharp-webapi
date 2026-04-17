using Microsoft.EntityFrameworkCore;
namespace dotnet_backend_2.Data.Entities;

[Index(nameof(Username), IsUnique = true)]

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
}
