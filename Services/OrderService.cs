using AutoMapper;
using AutoMapper.QueryableExtensions;
using dotnet_backend_2.Data;
using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.DTOs;
using Microsoft.EntityFrameworkCore;

namespace dotnet_backend_2.Services;

public class OrderService(ApplicationDbContext context, IMapper mapper) : IOrderService
{
    public async Task<OrderResponseDto?> CreateOrderFromCartAsync(int? userId, string? sessionId, CreateOrderFromCartDto orderDto)
    {
        // Validation: requires either userId or sessionId.
        if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        // Load cart with items (same query strategy as CartService).
        var cart = await context.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c =>
                (userId.HasValue && c.UserId == userId) ||
                (sessionId != null && c.SessionId == sessionId));

        // Ensure cart exists and contains items.
        if (cart == null || cart.Items.Count == 0)
        {
            return null; // Missing cart or empty cart.
        }

        // Build Order entity from cart data.
        var order = new Order
        {
            UserId = userId,
            SessionId = sessionId,
            Email = orderDto.Email,
            PhoneNumber = orderDto.PhoneNumber,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            ConfirmationToken = Guid.NewGuid().ToString(),
            ShippingAddress = new Address
            {
                Street = orderDto.ShippingAddress.Street,
                PostalCode = orderDto.ShippingAddress.PostalCode,
                City = orderDto.ShippingAddress.City
            },
            OrderItems = []
        };

        // Copy cart items and store price snapshot.
        decimal totalAmount = 0;
        foreach (var cartItem in cart.Items)
        {
            var unitPrice = cartItem.Product!.Price;
            var lineTotal = unitPrice * cartItem.Quantity;
            totalAmount += lineTotal;

            order.OrderItems.Add(new OrderItem
            {
                ProductId = cartItem.ProductId!.Value,
                Quantity = cartItem.Quantity,
                UnitPrice = unitPrice // Price at checkout time.
            });
        }
        order.TotalAmount = totalAmount;

        // Persist order.
        context.Orders.Add(order);

        // Remove cart after successful order creation.
        context.Carts.Remove(cart);

        await context.SaveChangesAsync();

        // Return mapped response.
        return await GetOrderByIdAsync(order.Id, userId, sessionId);
    }

    public async Task<List<OrderResponseDto>> GetOrdersAsync(int? userId, string? sessionId)
    {
        // Validation: requires either userId or sessionId.
        if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
        {
            return [];
        }

        var query = context.Orders
            .AsNoTracking()
            .Where(o => (userId.HasValue && o.UserId == userId) ||
                       (sessionId != null && o.SessionId == sessionId))
            .OrderByDescending(o => o.OrderDate);

        var orders = await query
            .ProjectTo<OrderResponseDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        return orders;
    }

    public async Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int? userId, string? sessionId)
    {
        // Validation: requires either userId or sessionId.
        if (!userId.HasValue && string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var order = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId &&
                       ((userId.HasValue && o.UserId == userId) ||
                        (sessionId != null && o.SessionId == sessionId)))
            .ProjectTo<OrderResponseDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return order;
    }

    public async Task<OrderResponseDto?> GetOrderByConfirmationTokenAsync(int orderId, string confirmationToken)
    {
        var expirationLimit = DateTime.UtcNow.AddDays(-7);

        var order = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId
                        && o.ConfirmationToken == confirmationToken
                        && o.OrderDate > expirationLimit)
            .ProjectTo<OrderResponseDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return order;
    }
}

