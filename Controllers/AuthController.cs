using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using dotnet_backend_2.DTOs;
using dotnet_backend_2.Services.Auth;
using dotnet_backend_2.Services;

namespace dotnet_backend_2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService, ICartService cartService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto registerDto)
    {
        var success = await authService.RegisterAsync(registerDto);
        if (!success)
        {
            return BadRequest("Username already exists.");
        }
        return Ok("User registered successfully.");
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto loginDto)
    {
        var (loginResponse, token) = await authService.LoginAsync(loginDto);
        if (loginResponse == null || token == null)
        {
            return Unauthorized("Invalid username or password.");
        }

        // Merge the session cart into the user cart when present.
        var sessionId = Request.Cookies["cartSessionId"];
        int mergedItems = 0;
        if (!string.IsNullOrEmpty(sessionId))
        {
            mergedItems = await cartService.MergeSessionCartToUserAsync(sessionId, loginResponse.UserId);
            Response.Cookies.Delete("cartSessionId");
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(1)
        };

        Response.Cookies.Append("jwt", token, cookieOptions);

        return Ok(new
        {
            loginResponse.Username,
            loginResponse.Role,
            loginResponse.UserId,
            CartItemsMerged = mergedItems
        });
    }


    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt");
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

        return Ok(new
        {
            username,
            userId,
            role,
        });
    }
}
