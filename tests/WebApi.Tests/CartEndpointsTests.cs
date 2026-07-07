using System.Net;
using System.Net.Http.Json;
using Shouldly;
using WebApi.DTOs;
using WebApi.Tests.Infrastructure;

namespace WebApi.Tests;

public class CartEndpointsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    /// Adds a seeded product to the client's session cart and returns the cart item.
    private async Task<CartItemDto> AddItemAsync(HttpClient client, int quantity = 2)
    {
        var product = await GetSeededProductAsync();
        var response = await client.PostAsJsonAsync(
            "/api/cart",
            new AddToCartDto { ProductId = product.Id, Quantity = quantity }
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        return cart!.Items.Single(i => i.ProductId == product.Id);
    }

    [Fact]
    public async Task AddToCart_ValidProduct_ReturnsCartWithItem()
    {
        // Anonymous client: the cart is tracked via the session cookie, which the
        // HttpClient keeps automatically between requests.
        var client = CreateClient();

        var item = await AddItemAsync(client, quantity: 2);

        item.Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task AddToCart_UnknownProduct_ReturnsBadRequest()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/cart",
            new AddToCartDto { ProductId = 999999, Quantity = 1 }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCart_AfterAddingItem_ReflectsItem()
    {
        var client = CreateClient();

        var item = await AddItemAsync(client, quantity: 3);
        var cart = await client.GetFromJsonAsync<CartDto>("/api/cart");

        cart.ShouldNotBeNull();
        cart.Items.ShouldContain(i => i.ProductId == item.ProductId && i.Quantity == 3);
        cart.TotalItems.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task UpdateCartItem_ValidQuantity_UpdatesQuantity()
    {
        var client = CreateClient();
        var item = await AddItemAsync(client, quantity: 2);

        var response = await client.PatchAsJsonAsync(
            $"/api/cart/{item.Id}",
            new UpdateCartItemDto { Quantity = 5 }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>();
        cart!.Items.Single(i => i.Id == item.Id).Quantity.ShouldBe(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task UpdateCartItem_NonPositiveQuantity_ReturnsBadRequest(int quantity)
    {
        var client = CreateClient();
        var item = await AddItemAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"/api/cart/{item.Id}",
            new UpdateCartItemDto { Quantity = quantity }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCartItem_UnknownItem_ReturnsNotFound()
    {
        var client = CreateClient();
        await AddItemAsync(client); // Ensure the session has a cart.

        var response = await client.PatchAsJsonAsync(
            "/api/cart/999999",
            new UpdateCartItemDto { Quantity = 1 }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCartItem_ExistingItem_RemovesIt()
    {
        var client = CreateClient();
        var item = await AddItemAsync(client);

        var response = await client.DeleteAsync($"/api/cart/{item.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var cart = await client.GetFromJsonAsync<CartDto>("/api/cart");
        cart!.Items.ShouldNotContain(i => i.Id == item.Id);
    }

    [Fact]
    public async Task DeleteCartItem_UnknownItem_ReturnsNotFound()
    {
        var client = CreateClient();
        await AddItemAsync(client); // Ensure the session has a cart.

        var response = await client.DeleteAsync("/api/cart/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
