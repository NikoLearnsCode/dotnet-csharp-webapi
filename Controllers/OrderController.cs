using dotnet_backend_2.DTOs;
using dotnet_backend_2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dotnet_backend_2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(IOrderService orderService) : ControllerBase
{
    // Reuse the same identity strategy as CartController.
    private (int? userId, string? sessionId) GetIdentifier()
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

        // If anonymous, fall back to the session cookie (same as cart flow).
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

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderResponseDto>> Checkout(CreateOrderFromCartDto orderDto)
    {
        var (userId, sessionId) = GetIdentifier();
        var order = await orderService.CreateOrderFromCartAsync(userId, sessionId, orderDto);

        if (order == null)
        {
            return BadRequest("Could not create order. Cart is empty or products unavailable.");
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderResponseDto>>> GetOrders()
    {
        var (userId, sessionId) = GetIdentifier();
        var orders = await orderService.GetOrdersAsync(userId, sessionId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrderById(int id)
    {
        var (userId, sessionId) = GetIdentifier();
        var order = await orderService.GetOrderByIdAsync(id, userId, sessionId);

        if (order == null)
        {
            return NotFound($"Order {id} not found.");
        }

        return Ok(order);
    }

    [HttpGet("{id}/confirm")]
    [AllowAnonymous]
    public async Task<ActionResult<OrderResponseDto>> GetOrderByConfirmationToken(int id, [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest("Confirmation token is required.");
        }

        var order = await orderService.GetOrderByConfirmationTokenAsync(id, token);

        if (order == null)
        {
            return NotFound("Order not found or invalid confirmation token.");
        }

        return Ok(order);
    }
}




