using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using WebApi.DTOs;
using WebApi.Services.Auth;
using WebApi.Tests.Infrastructure;

namespace WebApi.Tests;

/// <summary>
/// Refresh-token rotation over the real JwtBearer pipeline: /refresh rotates the
/// pair, a replayed (already-rotated) token is rejected and kills the whole
/// session family, and logout revokes server-side. Separate class from
/// <see cref="JwtAuthFlowTests"/> so each factory gets its own 10-requests/min
/// "auth" rate-limit window.
/// </summary>
public class RefreshTokenFlowTests(JwtWebApplicationFactory factory)
    : IClassFixture<JwtWebApplicationFactory>
{
    private HttpClient CreateClient() =>
        factory.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }
        );

    private static Task<HttpResponseMessage> LoginAsAdminAsync(HttpClient client) =>
        client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Username = "admin", Password = "Admin123" }
        );

    private static string? GetSetCookieValue(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies
                .FirstOrDefault(c => c.StartsWith($"{name}="))
                ?.Split(';')[0][$"{name}=".Length..]
            : null;

    [Fact]
    public async Task Refresh_RotatesBothCookies()
    {
        var client = CreateClient();
        var login = await LoginAsAdminAsync(client);
        var initialJwt = GetSetCookieValue(login, AuthCookie.Name);
        var initialRefresh = GetSetCookieValue(login, AuthCookie.RefreshName);

        var response = await client.PostAsync("/api/auth/refresh", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var newJwt = GetSetCookieValue(response, AuthCookie.Name);
        var newRefresh = GetSetCookieValue(response, AuthCookie.RefreshName);
        newJwt.ShouldNotBeNullOrEmpty();
        newRefresh.ShouldNotBeNullOrEmpty();
        newJwt.ShouldNotBe(initialJwt);
        newRefresh.ShouldNotBe(initialRefresh);
    }

    [Fact]
    public async Task ReplayedRefreshToken_IsRejected_AndRevokesTheFamily()
    {
        var client = CreateClient();
        var login = await LoginAsAdminAsync(client);
        var firstRefreshToken = GetSetCookieValue(login, AuthCookie.RefreshName);
        firstRefreshToken.ShouldNotBeNullOrEmpty();

        // Rotate: the client's cookie container now holds the successor token.
        (await client.PostAsync("/api/auth/refresh", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Replaying the rotated-away token must fail...
        var replayClient = CreateClient();
        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            $"{AuthCookie.RefreshName}={firstRefreshToken}"
        );
        var replay = await replayClient.PostAsync("/api/auth/refresh", null);
        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // ...and revoke every active token for the user, including the successor.
        var afterReplay = await client.PostAsync("/api/auth/refresh", null);
        afterReplay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsync("/api/auth/refresh", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_SubsequentRefreshUnauthorized()
    {
        var client = CreateClient();
        var login = await LoginAsAdminAsync(client);
        var refreshToken = GetSetCookieValue(login, AuthCookie.RefreshName);
        refreshToken.ShouldNotBeNullOrEmpty();

        (await client.PostAsync("/api/auth/logout", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Logout cleared the client's cookies; present the old token explicitly
        // to prove it was revoked server-side, not just deleted from the browser.
        var replayClient = CreateClient();
        replayClient.DefaultRequestHeaders.Add(
            "Cookie",
            $"{AuthCookie.RefreshName}={refreshToken}"
        );
        var response = await replayClient.PostAsync("/api/auth/refresh", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
