using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using WebApi.DTOs;
using WebApi.Services.Auth;
using WebApi.Tests.Infrastructure;

namespace WebApi.Tests;

/// <summary>
/// Exercises the real JwtBearer pipeline (login → Set-Cookie → cookie extraction in
/// OnMessageReceived → signature/lifetime validation) instead of TestAuthHandler.
/// The refresh-token flow lives in <see cref="RefreshTokenFlowTests"/>, which uses
/// its own factory so the two classes don't share the "auth" rate-limit window.
/// </summary>
public class JwtAuthFlowTests(JwtWebApplicationFactory factory)
    : IClassFixture<JwtWebApplicationFactory>
{
    // In Development the Secure flag follows the request scheme (see AuthCookie),
    // so an https base address both yields Secure cookies (keeping the asserts
    // below meaningful for production) and lets the CookieContainer send them back.
    private HttpClient CreateClient() =>
        factory.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") }
        );

    private static Task<HttpResponseMessage> LoginAsAdminAsync(HttpClient client) =>
        client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Username = "admin", Password = "Admin123" }
        );

    private static async Task<int> GetLeafCategoryIdAsync(HttpClient client)
    {
        var tree = await client.GetFromJsonAsync<List<CategoryTreeDto>>("/api/categories/tree");
        return Flatten(tree!).First(c => c.Type == "LEAF").Id;
    }

    private static IEnumerable<CategoryTreeDto> Flatten(IEnumerable<CategoryTreeDto> nodes) =>
        nodes.SelectMany(n => new[] { n }.Concat(Flatten(n.Children)));

    private static async Task<CreateProductDto> NewProductAsync(HttpClient client, string name) =>
        new()
        {
            Name = name,
            Description = "Created by JWT flow test",
            Price = 100m,
            ImageUrl = "/images/products/test.jpg",
            CategoryIds = [await GetLeafCategoryIdAsync(client)],
        };

    [Fact]
    public async Task Login_SetsHttpOnlyAccessAndRefreshCookies()
    {
        var client = CreateClient();

        var response = await LoginAsAdminAsync(client);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var setCookies = response.Headers.GetValues("Set-Cookie").ToList();

        var accessCookie = setCookies.FirstOrDefault(c => c.StartsWith($"{AuthCookie.Name}="));
        accessCookie.ShouldNotBeNull();
        accessCookie.ToLower().ShouldContain("httponly");
        accessCookie.ToLower().ShouldContain("secure");

        var refreshCookie = setCookies.FirstOrDefault(c =>
            c.StartsWith($"{AuthCookie.RefreshName}=")
        );
        refreshCookie.ShouldNotBeNull();
        refreshCookie.ToLower().ShouldContain("httponly");
        refreshCookie.ToLower().ShouldContain("secure");
        refreshCookie.ToLower().ShouldContain($"path={AuthCookie.RefreshPath}");
    }

    [Fact]
    public async Task Login_OverHttp_SetsCookiesWithoutSecureFlag()
    {
        // WebApplicationFactory hosts the app as Development, where Secure follows
        // the request scheme: over plain http the cookies must not carry the flag,
        // so strict non-browser clients (Insomnia et al.) send them back.
        var client = factory.CreateClient(); // default http://localhost base address

        var response = await LoginAsAdminAsync(client);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var setCookies = response.Headers.GetValues("Set-Cookie").ToList();
        setCookies.ShouldContain(c => c.StartsWith($"{AuthCookie.Name}="));
        foreach (var cookie in setCookies)
        {
            var attributes = cookie.Split(';').Skip(1).Select(a => a.Trim().ToLower());
            attributes.ShouldNotContain("secure");
            attributes.ShouldContain("httponly");
        }
    }

    [Fact]
    public async Task JwtCookie_AuthenticatesAdminEndpoint()
    {
        var client = CreateClient();
        (await LoginAsAdminAsync(client)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // The cookie set by login (kept by the HttpClient) must satisfy [Authorize(Roles = "Admin")].
        var response = await client.PostAsJsonAsync(
            "/api/products",
            await NewProductAsync(client, "Jwt Flow Product")
        );

        response.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            "WWW-Authenticate: "
                + string.Join(" | ", response.Headers.WwwAuthenticate.Select(h => h.ToString()))
        );
    }

    [Fact]
    public async Task WithoutCookie_AdminEndpointReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/products",
            await NewProductAsync(client, "No Cookie Product")
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TamperedJwtCookie_IsRejected()
    {
        var client = CreateClient();
        var login = await LoginAsAdminAsync(client);
        var token = login
            .Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith($"{AuthCookie.Name}="))
            .Split(';')[0][$"{AuthCookie.Name}=".Length..];

        // Flip the last character of the signature to break it.
        var lastChar = token[^1] == 'a' ? 'b' : 'a';
        var tampered = token[..^1] + lastChar;

        var bypassClient = CreateClient();
        bypassClient.DefaultRequestHeaders.Add("Cookie", $"{AuthCookie.Name}={tampered}");
        var response = await bypassClient.PostAsJsonAsync(
            "/api/products",
            await NewProductAsync(bypassClient, "Tampered Product")
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ClearsCookie_SubsequentAdminCallUnauthorized()
    {
        var client = CreateClient();
        (await LoginAsAdminAsync(client)).StatusCode.ShouldBe(HttpStatusCode.OK);

        (await client.PostAsync("/api/auth/logout", null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync(
            "/api/products",
            await NewProductAsync(client, "After Logout Product")
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
