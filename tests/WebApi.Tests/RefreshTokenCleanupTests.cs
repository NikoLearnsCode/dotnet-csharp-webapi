using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WebApi.Data;
using WebApi.Data.Entities;
using WebApi.DTOs;
using WebApi.Tests.Infrastructure;

namespace WebApi.Tests;

/// <summary>
/// Expired refresh-token rows are pruned when new tokens are issued (login/refresh),
/// while revoked-but-unexpired rows survive - they are the replay-detection signal.
/// Own class (not part of RefreshTokenFlowTests) so it gets its own factory and
/// therefore its own "auth" rate-limit window.
/// </summary>
public class RefreshTokenCleanupTests(CustomWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private const int AdminUserId = 1; // Seeded admin (SeedData).

    [Fact]
    public async Task Login_DeletesExpiredRows_KeepsRevokedUnexpiredRows()
    {
        var now = DateTime.UtcNow;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.RefreshTokens.AddRange(
                // Expired (and long since revoked): should be pruned on next issue.
                new RefreshToken
                {
                    TokenHash = "expired-row",
                    UserId = AdminUserId,
                    CreatedAtUtc = now.AddDays(-14),
                    ExpiresAtUtc = now.AddDays(-7),
                    RevokedAtUtc = now.AddDays(-14),
                },
                // Revoked but not expired: must survive (replay detection).
                new RefreshToken
                {
                    TokenHash = "revoked-active-row",
                    UserId = AdminUserId,
                    CreatedAtUtc = now.AddDays(-1),
                    ExpiresAtUtc = now.AddDays(6),
                    RevokedAtUtc = now.AddDays(-1),
                }
            );
            await db.SaveChangesAsync();
        }

        var login = await CreateClient()
            .PostAsJsonAsync(
                "/api/auth/login",
                new LoginDto { Username = "admin", Password = "Admin123" }
            );
        login.StatusCode.ShouldBe(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hashes = await db
                .RefreshTokens.Where(rt => rt.UserId == AdminUserId)
                .Select(rt => rt.TokenHash)
                .ToListAsync();

            hashes.ShouldNotContain("expired-row");
            hashes.ShouldContain("revoked-active-row");
            // The login itself issued one new (active) token.
            hashes.Count.ShouldBe(2);
        }
    }
}
