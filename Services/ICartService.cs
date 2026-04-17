using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Services;

public interface ICartService
{
    Task<CartDto?> GetCartAsync(int? userId, string? sessionId);
    Task<CartDto?> AddToCartAsync(int? userId, string? sessionId, AddToCartDto itemDto);
    Task<CartDto?> UpdateCartItemAsync(int? userId, string? sessionId, int cartItemId, UpdateCartItemDto updateDto);
    Task<bool> DeleteCartItemAsync(int? userId, string? sessionId, int cartItemId);
    Task<int> MergeSessionCartToUserAsync(string sessionId, int userId);
}
