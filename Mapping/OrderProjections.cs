using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Mapping;

public static class OrderProjections
{
    public static IQueryable<OrderResponseDto> ProjectToDto(this IQueryable<Order> orders) =>
        orders.Select(order => new OrderResponseDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            ConfirmationToken = order.ConfirmationToken,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            UserId = order.UserId,
            SessionId = order.SessionId,
            Email = order.Email,
            PhoneNumber = order.PhoneNumber,
            ShippingAddress = new AddressDto
            {
                Street = order.ShippingAddress.Street,
                PostalCode = order.ShippingAddress.PostalCode,
                City = order.ShippingAddress.City
            },
            OrderItems = order.OrderItems.Select(item => new OrderItemResponseDto
            {
                Id = item.Id,
                ProductId = item.ProductId,

                ProductName = item.Product.Name,
                ProductUrlSlug = item.Product.UrlSlug,
                ProductImageUrl = item.Product.ImageUrl,

                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.UnitPrice * item.Quantity
            }).ToList()
        });
}
