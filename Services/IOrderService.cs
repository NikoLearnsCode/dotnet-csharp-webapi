using WebApi.DTOs;

namespace WebApi.Services;

public interface IOrderService
{
    Task<OrderResponseDto?> CreateOrderFromCartAsync(
        int? userId,
        string? sessionId,
        CreateOrderFromCartDto orderDto
    );
    Task<List<OrderResponseDto>> GetOrdersAsync(int? userId, string? sessionId);
    Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int? userId, string? sessionId);
    Task<OrderResponseDto?> GetOrderByConfirmationTokenAsync(int orderId, string confirmationToken);
}
