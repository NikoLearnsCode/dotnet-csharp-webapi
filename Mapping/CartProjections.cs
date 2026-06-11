using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Mapping;

public static class CartProjections
{
    public static IQueryable<CartDto> ProjectToDto(this IQueryable<Cart> carts) =>
        carts.Select(cart => new CartDto
        {
            Id = cart.Id,
            Items = cart.Items.Select(item => new CartItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId!.Value,

                ProductName = item.Product!.Name,
                ProductUrlSlug = item.Product!.UrlSlug,
                ProductPrice = item.Product!.Price,
                ProductImageUrl = item.Product!.ImageUrl,

                Quantity = item.Quantity,
                LineTotal = item.Product!.Price * item.Quantity
            }).ToList(),
            TotalItems = cart.Items.Sum(item => item.Quantity),
            SubTotal = cart.Items.Sum(item => item.Product!.Price * item.Quantity)
        });
}
