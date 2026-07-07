using System.Net;
using System.Net.Http.Json;
using Shouldly;
using WebApi.DTOs;
using WebApi.Tests.Infrastructure;

namespace WebApi.Tests;

public class OrderEndpointsTests(CustomWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    private static CreateOrderFromCartDto NewCheckoutDto() =>
        new()
        {
            Email = "buyer@example.com",
            ShippingAddress = new AddressDto
            {
                Street = "Main St 1",
                PostalCode = "12345",
                City = "Stockholm",
            },
        };

    /// Fills the client's session cart with two seeded products and returns the
    /// expected order total (price snapshot × quantity).
    private async Task<decimal> FillCartAsync(HttpClient client)
    {
        var first = await GetSeededProductAsync(0);
        var second = await GetSeededProductAsync(1);

        await client.PostAsJsonAsync(
            "/api/cart",
            new AddToCartDto { ProductId = first.Id, Quantity = 2 }
        );
        await client.PostAsJsonAsync(
            "/api/cart",
            new AddToCartDto { ProductId = second.Id, Quantity = 1 }
        );

        return first.Price * 2 + second.Price;
    }

    [Fact]
    public async Task GetOrders_FreshSession_ReturnsEmptyList()
    {
        var client = CreateClient();

        var orders = await client.GetFromJsonAsync<List<OrderResult>>("/api/orders");

        orders.ShouldNotBeNull();
        orders.ShouldBeEmpty();
    }

    [Fact]
    public async Task Checkout_WithEmptyCart_ReturnsBadRequest()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders/checkout", NewCheckoutDto());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Checkout_WithFilledCart_CreatesOrderAndEmptiesCart()
    {
        var client = CreateClient();
        var expectedTotal = await FillCartAsync(client);

        var response = await client.PostAsJsonAsync("/api/orders/checkout", NewCheckoutDto());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var order = await response.Content.ReadFromJsonAsync<OrderResult>();
        order.ShouldNotBeNull();
        order.OrderItems.Count.ShouldBe(2);
        order.TotalAmount.ShouldBe(Math.Round(expectedTotal, 2));
        order.OrderItems.ShouldAllBe(i => i.UnitPrice > 0);
        order.Email.ShouldBe("buyer@example.com");

        // The cart must be consumed by the checkout. A deleted cart serializes as
        // Ok(null), which the framework turns into 204 with an empty body.
        var cartResponse = await client.GetAsync("/api/cart");
        if (cartResponse.StatusCode != HttpStatusCode.NoContent)
        {
            var cart = await cartResponse.Content.ReadFromJsonAsync<CartDto?>();
            (cart == null || cart.Items.Count == 0).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task GetOrderById_OwnOrder_ReturnsOrder()
    {
        var client = CreateClient();
        await FillCartAsync(client);
        var checkout = await client.PostAsJsonAsync("/api/orders/checkout", NewCheckoutDto());
        var created = await checkout.Content.ReadFromJsonAsync<OrderResult>();

        var response = await client.GetAsync($"/api/orders/{created!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderResult>();
        order!.Id.ShouldBe(created.Id);
    }

    [Fact]
    public async Task GetOrderById_OtherSession_ReturnsNotFound()
    {
        var client = CreateClient();
        await FillCartAsync(client);
        var checkout = await client.PostAsJsonAsync("/api/orders/checkout", NewCheckoutDto());
        var created = await checkout.Content.ReadFromJsonAsync<OrderResult>();

        // A different session (new client, new cookie) must not see the order.
        var stranger = CreateClient();
        var response = await stranger.GetAsync($"/api/orders/{created!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConfirmOrder_WithValidToken_ReturnsOrder()
    {
        var client = CreateClient();
        await FillCartAsync(client);
        var checkout = await client.PostAsJsonAsync("/api/orders/checkout", NewCheckoutDto());
        var created = await checkout.Content.ReadFromJsonAsync<OrderResult>();

        // The confirmation endpoint is anonymous - use a fresh client on purpose.
        var response = await CreateClient()
            .GetAsync($"/api/orders/{created!.Id}/confirm?token={created.ConfirmationToken}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConfirmOrder_WithWrongToken_ReturnsNotFound()
    {
        var client = CreateClient();
        await FillCartAsync(client);
        var checkout = await client.PostAsJsonAsync("/api/orders/checkout", NewCheckoutDto());
        var created = await checkout.Content.ReadFromJsonAsync<OrderResult>();

        var response = await CreateClient()
            .GetAsync($"/api/orders/{created!.Id}/confirm?token={Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
