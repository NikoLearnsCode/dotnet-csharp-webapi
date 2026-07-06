using Microsoft.EntityFrameworkCore;

namespace WebApi.Data.Entities;

// Only the SHA-256 hash of the token is stored; the raw value lives solely in
// the client's cookie, so a database leak cannot be replayed as a session.
[Index(nameof(TokenHash), IsUnique = true)]
public class RefreshToken
{
    public int Id { get; set; }
    public required string TokenHash { get; init; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime ExpiresAtUtc { get; init; }

    // Set on logout and on rotation, so it stays mutable.
    public DateTime? RevokedAtUtc { get; set; }

    // Hash of the token that replaced this one on rotation. A revoked token
    // being presented again means the raw value leaked (replay), which
    // triggers revocation of all the user's active tokens.
    public string? ReplacedByTokenHash { get; set; }
}
