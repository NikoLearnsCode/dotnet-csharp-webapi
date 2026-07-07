using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using WebApi.DTOs;
using WebApi.Services;
using WebApi.Services.Auth;

namespace WebApi.Controllers;

[ApiController]
[Route(AuthCookie.RoutePrefix)]
public class AuthController(
    IAuthService authService,
    ICartService cartService,
    IOptions<JwtOptions> jwtOptions,
    IWebHostEnvironment environment
) : ControllerBase
{
    // Secure follows the request scheme in Development so the JWT flow is testable
    // over plain http in strict clients (Insomnia et al.; browsers already treat
    // http://localhost as trustworthy). Outside Development it is unconditional -
    // see the rationale in AuthCookie.
    private bool SecureCookies => !environment.IsDevelopment() || Request.IsHttps;

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        var success = await authService.RegisterAsync(registerDto);
        if (!success)
        {
            return Problem(
                detail: "Username already exists.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new { Message = "User registered successfully." }
        );
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var (loginResponse, tokens) = await authService.LoginAsync(loginDto);
        if (loginResponse == null || tokens == null)
        {
            return Problem(
                detail: "Invalid username or password.",
                statusCode: StatusCodes.Status401Unauthorized
            );
        }

        // Merge the session cart into the user cart when present.
        var sessionId = Request.Cookies["cartSessionId"];
        var mergedItems = 0;
        if (!string.IsNullOrEmpty(sessionId))
        {
            mergedItems = await cartService.MergeSessionCartToUserAsync(
                sessionId,
                loginResponse.UserId
            );
            Response.Cookies.Delete("cartSessionId");
        }

        SetAuthCookies(tokens);

        return Ok(
            new
            {
                loginResponse.Username,
                loginResponse.Role,
                loginResponse.UserId,
                CartItemsMerged = mergedItems,
            }
        );
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh()
    {
        var rawRefreshToken = Request.Cookies[AuthCookie.RefreshName];
        if (!string.IsNullOrEmpty(rawRefreshToken))
        {
            var tokens = await authService.RefreshAsync(rawRefreshToken);
            if (tokens is not null)
            {
                SetAuthCookies(tokens);
                return Ok(new { Message = "Token refreshed." });
            }
        }

        DeleteAuthCookies();
        return Problem(
            detail: "Invalid or expired refresh token.",
            statusCode: StatusCodes.Status401Unauthorized
        );
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync(Request.Cookies[AuthCookie.RefreshName]);
        DeleteAuthCookies();
        return Ok(new { Message = "Logged out successfully." });
    }

    [HttpGet("user")]
    [AllowAnonymous]
    public IActionResult GetProfile()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;

        // Return null when no authenticated user is available.
        if (string.IsNullOrEmpty(username))
        {
            return Ok(null);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(
            new
            {
                username,
                userId,
                role,
            }
        );
    }

    private void SetAuthCookies(AuthTokens tokens)
    {
        var options = jwtOptions.Value;
        Response.Cookies.Append(
            AuthCookie.Name,
            tokens.AccessToken,
            AuthCookie.AccessOptions(options.AccessTokenLifetime, SecureCookies)
        );
        Response.Cookies.Append(
            AuthCookie.RefreshName,
            tokens.RefreshToken,
            AuthCookie.RefreshOptions(options.RefreshTokenLifetime, SecureCookies)
        );
    }

    private void DeleteAuthCookies()
    {
        // Deleting a path-scoped cookie requires the same attributes it was set with.
        Response.Cookies.Delete(
            AuthCookie.Name,
            AuthCookie.AccessOptions(TimeSpan.Zero, SecureCookies)
        );
        Response.Cookies.Delete(
            AuthCookie.RefreshName,
            AuthCookie.RefreshOptions(TimeSpan.Zero, SecureCookies)
        );
    }
}
