using System.Net;
using System.Net.Http.Json;
using Shouldly;
using WebApi.DTOs;
using WebApi.Tests.Infrastructure;

namespace WebApi.Tests;

public class AuthEndpointsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsUserInfo()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Username = "admin", Password = "Admin123" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body.ShouldNotBeNull();
        body.Username.ShouldBe("admin");
        body.Role.ShouldBe("Admin");
        body.UserId.ShouldBe(1);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Username = "admin", Password = "definitely-wrong" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_NewUser_ReturnsCreated()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto { Username = "newuser", Password = "Password123" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_NewUser_CanLoginWithUserRole()
    {
        var client = CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        const string password = "Password123";

        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto { Username = username, Password = password }
        );
        register.StatusCode.ShouldBe(HttpStatusCode.Created);

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Username = username, Password = password }
        );

        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<LoginResult>();
        body.ShouldNotBeNull();
        body.Username.ShouldBe(username);
        body.Role.ShouldBe("User");
        body.UserId.ShouldNotBe(1); // seeded admin
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsBadRequest()
    {
        var client = CreateClient();

        // "admin" is seeded, so registering it again must be rejected.
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto { Username = "admin", Password = "Password123" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithSessionCart_MergesItIntoUserCart()
    {
        // Anonymous client builds a session cart (cookie kept by the HttpClient),
        // then logs in - the login response must report the merged quantity.
        var client = CreateClient();
        var product = await GetSeededProductAsync();

        await client.PostAsJsonAsync(
            "/api/cart",
            new AddToCartDto { ProductId = product.Id, Quantity = 3 }
        );

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto { Username = "admin", Password = "Admin123" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResult>();
        body.ShouldNotBeNull();
        body.CartItemsMerged.ShouldBe(3);
    }
}
