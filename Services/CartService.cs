using System.Net.Mime;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using dotnet_backend_2.Data;
using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.DTOs;

using Microsoft.EntityFrameworkCore;

namespace dotnet_backend_2.Services
{
    public class CartService(ApplicationDbContext context, IMapper mapper) : ICartService
    {
        public async Task<CartDto?> GetCartAsync(int? userId, string? sessionId)
        {
            var cartQuery = context.Carts
                .AsNoTracking()
                .Where(c => (userId.HasValue && c.UserId == userId) ||
                           (sessionId != null && c.SessionId == sessionId));

            var cartDto = await cartQuery
                .ProjectTo<CartDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            return cartDto;
        }

        public async Task<CartDto?> AddToCartAsync(int? userId, string? sessionId, AddToCartDto itemDto)
        {
            //  Validate that the product exists.
            var productExist = await context.Products.AnyAsync(p => p.Id == itemDto.ProductId);
            if (!productExist) return null;

            // Fetch the cart.
            var cart = await context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => (userId.HasValue && c.UserId == userId) || (sessionId != null && c.SessionId == sessionId));

            // Create a new cart if none exists.
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    SessionId = sessionId,
                    Items = []
                };
                context.Carts.Add(cart);
            }
            // Update existing item quantity or create a new cart item.
            var existingItem = cart.Items.FirstOrDefault(item => item.ProductId == itemDto.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += itemDto.Quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,

                };
                cart.Items.Add(newItem);
            }
            // Save and return updated cart.
            await context.SaveChangesAsync();
            return await GetCartAsync(userId, sessionId);
        }

        public async Task<CartDto?> UpdateCartItemAsync(int? userId, string? sessionId, int cartItemId, UpdateCartItemDto updateDto)
        {
            // Find cart item.
            var cartItem = await context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId &&
                    ((userId.HasValue && ci.Cart!.UserId == userId) ||
                     (sessionId != null && ci.Cart!.SessionId == sessionId)));

            if (cartItem == null) return null;

            // Update quantity.
            cartItem.Quantity = updateDto.Quantity;

            // Save and return updated cart.
            await context.SaveChangesAsync();
            return await GetCartAsync(userId, sessionId);
        }

        public async Task<bool> DeleteCartItemAsync(int? userId, string? sessionId, int cartItemId)
        {
            // Find cart item.
            var cartItem = await context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId &&
                    ((userId.HasValue && ci.Cart!.UserId == userId) ||
                     (sessionId != null && ci.Cart!.SessionId == sessionId)));

            if (cartItem == null) return false;

            // Remove cart item.
            context.CartItems.Remove(cartItem);

            // Save changes.
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MergeSessionCartToUserAsync(string sessionId, int userId)
        {
            // Load session cart with items into memory.
            var sessionCart = await context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);

            // Verify session cart exists.
            if (sessionCart == null || sessionCart.Items.Count == 0) return 0;

            // Calculate how many units will be merged.
            // var mergedItemsCount = sessionCart.Items.Count;
            var mergedItemsCount = sessionCart.Items.Sum(item => item.Quantity); // Count total quantity.

            // Load user cart with items into memory.
            var userCart = await context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            // Check whether the user cart exists.
            if (userCart == null)
            {
                // Convert session cart into user cart.
                sessionCart.UserId = userId;
                sessionCart.SessionId = null;
            }
            else
            {
                // Iterate items in memory (no DB calls here).
                foreach (var sessionItem in sessionCart.Items)
                {
                    // Search for matching item in loaded user cart.
                    var existingItem = userCart.Items
                        .FirstOrDefault(i => i.ProductId == sessionItem.ProductId);

                    if (existingItem != null)
                    {
                        // Update quantity in memory.
                        existingItem.Quantity += sessionItem.Quantity;
                    }
                    else
                    {
                        // Add new item to in-memory collection.
                        userCart.Items.Add(new CartItem
                        {
                            ProductId = sessionItem.ProductId,
                            Quantity = sessionItem.Quantity
                        });
                    }
                }

                // Mark session cart for removal.
                context.Carts.Remove(sessionCart);
            }

            // Persist all in-memory changes in one save.
            await context.SaveChangesAsync();
            return mergedItemsCount;
        }
    }

}
