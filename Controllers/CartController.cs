using dotnet_backend_2.DTOs;
using dotnet_backend_2.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dotnet_backend_2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController(ICartService cartService) : ControllerBase
{
    private (int? userId, string? sessionId) GetCartIdentifier()
    {
        // If authenticated, prefer the user ID from claims.
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                return (userId, null);
            }
        }

        // Otherwise identify the cart by session cookie.
        var sessionId = Request.Cookies["cartSessionId"];
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            Response.Cookies.Append("cartSessionId", sessionId, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        return (null, sessionId);
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var (userId, sessionId) = GetCartIdentifier();
        var cartDto = await cartService.GetCartAsync(userId, sessionId);

        if (cartDto == null)
        {
            return Ok(null);
        }

        return Ok(cartDto);
    }

    [HttpPost]
    public async Task<ActionResult<CartDto>> AddToCart(AddToCartDto itemDto)
    {
        var (userId, sessionId) = GetCartIdentifier();
        var updatedCart = await cartService.AddToCartAsync(userId, sessionId, itemDto);

        if (updatedCart == null)
        {
            return BadRequest("Could not add item. Product not found.");
        }

        return Ok(updatedCart);
    }

    [HttpPatch("{cartItemId}")]
    public async Task<IActionResult> UpdateCartItem(int cartItemId, UpdateCartItemDto updateDto)
    {
        var (userId, sessionId) = GetCartIdentifier();
        var updatedCart = await cartService.UpdateCartItemAsync(userId, sessionId, cartItemId, updateDto);

        if (updatedCart == null)
        {
            return BadRequest($"Could not update cart item. Item {cartItemId} not found in your cart.");
        }

        return Ok(updatedCart);
    }

    [HttpDelete("{cartItemId}")]
    public async Task<IActionResult> DeleteCartItem(int cartItemId)
    {
        var (userId, sessionId) = GetCartIdentifier();
        await cartService.DeleteCartItemAsync(userId, sessionId, cartItemId);
        return NoContent();
    }
}

