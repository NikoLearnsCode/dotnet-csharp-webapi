using System.Net;
using System.Net.Http.Json;
using Shouldly;
using WebApi.DTOs;
using WebApi.Tests.Infrastructure;

namespace WebApi.Tests;

public class ProductsEndpointsTests(CustomWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private static CreateProductDto NewProduct(string name, int categoryId) =>
        new()
        {
            Name = name,
            Description = "Created by integration test",
            Price = 149.50m,
            ImageUrl = "/images/products/test.jpg",
            CategoryIds = [categoryId],
        };

    /// Creates a product as admin and returns it.
    private async Task<ProductDto> CreateProductAsync(string name)
    {
        var response = await CreateAdminClient()
            .PostAsJsonAsync("/api/products", NewProduct(name, await GetLeafCategoryIdAsync()));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    [Fact]
    public async Task GetProducts_ReturnsPagedListWithDefaultPageSize()
    {
        var client = CreateClient();

        var page = await client.GetFromJsonAsync<PagedProducts>("/api/products");

        page.ShouldNotBeNull();
        page.PageSize.ShouldBe(10);
        page.Items.Count.ShouldBe(10);
        page.TotalCount.ShouldBeGreaterThan(10);
    }

    [Fact]
    public async Task GetProducts_WithDisallowedPageSize_FallsBackToDefault()
    {
        var client = CreateClient();

        var page = await client.GetFromJsonAsync<PagedProducts>("/api/products?pageSize=999");

        page.ShouldNotBeNull();
        page.PageSize.ShouldBe(10);
        page.Items.Count.ShouldBe(10);
    }

    [Fact]
    public async Task GetProducts_WithNonPositivePage_FallsBackToFirstPage()
    {
        var client = CreateClient();

        // Regression: page=0 used to produce a negative OFFSET and a 500.
        var response = await client.GetAsync("/api/products?page=0");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedProducts>();
        page!.CurrentPage.ShouldBe(1);
        page.Items.Count.ShouldBe(10);
    }

    [Fact]
    public async Task GetProductById_Existing_ReturnsProduct()
    {
        var client = CreateClient();
        var seeded = await GetSeededProductAsync();

        var response = await client.GetAsync($"/api/products/{seeded.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.ShouldNotBeNull();
        product.Id.ShouldBe(seeded.Id);
    }

    [Fact]
    public async Task GetProductById_Missing_ReturnsNotFound()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/products/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProductsCursor_ReturnsItemsAndNextCursor()
    {
        var client = CreateClient();

        var result = await client.GetFromJsonAsync<CursorPagedList<ProductDto>>(
            "/api/products/cursor?limit=5"
        );

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(5);
        result.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetProductsCursor_WithNonPositiveLimit_FallsBackToDefault()
    {
        var client = CreateClient();

        // Regression: limit=0 used to crash with an index out of range and a 500.
        var response = await client.GetAsync("/api/products/cursor?limit=0");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CursorPagedList<ProductDto>>();
        result!.Items.Count.ShouldBe(12); // DefaultCursorPageSize
    }

    [Fact]
    public async Task CreateProduct_AsAdmin_ReturnsCreated()
    {
        var created = await CreateProductAsync("Integration Test Sneaker");

        created.Name.ShouldBe("Integration Test Sneaker");
        created.Id.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CreateProduct_Anonymous_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/products",
            NewProduct("Anon Product", await GetLeafCategoryIdAsync())
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_AsNonAdmin_ReturnsForbidden()
    {
        var client = CreateUserClient();

        var response = await client.PostAsJsonAsync(
            "/api/products",
            NewProduct("User Product", await GetLeafCategoryIdAsync())
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProduct_InBranchCategory_ReturnsBadRequest()
    {
        var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            "/api/products",
            NewProduct("Branch Category Product", await GetBranchCategoryIdAsync())
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateName_ReturnsBadRequest()
    {
        var client = CreateAdminClient();
        var seeded = await GetSeededProductAsync();

        var response = await client.PostAsJsonAsync(
            "/api/products",
            NewProduct(seeded.Name, await GetLeafCategoryIdAsync())
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProduct_AsAdmin_UpdatesNameAndPrice()
    {
        var product = await CreateProductAsync("Patch Target");

        var response = await CreateAdminClient()
            .PatchAsJsonAsync(
                $"/api/products/{product.Id}",
                new UpdateProductDto { Name = "Patched Name", Price = 99.99m }
            );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();
        updated!.Name.ShouldBe("Patched Name");
        updated.Price.ShouldBe(99.99m);
    }

    [Fact]
    public async Task UpdateProduct_ToExistingName_ReturnsBadRequest()
    {
        var product = await CreateProductAsync("Rename Source");
        var seeded = await GetSeededProductAsync();

        var response = await CreateAdminClient()
            .PatchAsJsonAsync(
                $"/api/products/{product.Id}",
                new UpdateProductDto { Name = seeded.Name }
            );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteProduct_Unreferenced_ReturnsNoContentThenNotFound()
    {
        var product = await CreateProductAsync("Delete Target");
        var admin = CreateAdminClient();

        var response = await admin.DeleteAsync($"/api/products/{product.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await CreateClient().GetAsync($"/api/products/{product.Id}")).StatusCode.ShouldBe(
            HttpStatusCode.NotFound
        );
    }

    [Fact]
    public async Task DeleteProduct_InCart_SucceedsAndEmptiesCart()
    {
        var product = await CreateProductAsync("Carted Product");

        var shopper = CreateClient();
        await shopper.PostAsJsonAsync(
            "/api/cart",
            new AddToCartDto { ProductId = product.Id, Quantity = 1 }
        );

        var response = await CreateAdminClient().DeleteAsync($"/api/products/{product.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var cart = await shopper.GetFromJsonAsync<CartDto>("/api/cart");
        cart!.Items.ShouldNotContain(i => i.ProductId == product.Id);
    }

    [Fact]
    public async Task DeleteProduct_WithOrders_ReturnsConflict()
    {
        var product = await CreateProductAsync("Ordered Product");

        // Regression: this used to cascade-delete the order line silently.
        var shopper = CreateClient();
        await shopper.PostAsJsonAsync(
            "/api/cart",
            new AddToCartDto { ProductId = product.Id, Quantity = 1 }
        );
        var checkout = await shopper.PostAsJsonAsync(
            "/api/orders/checkout",
            new CreateOrderFromCartDto
            {
                Email = "buyer@example.com",
                ShippingAddress = new AddressDto
                {
                    Street = "Main St 1",
                    PostalCode = "12345",
                    City = "Stockholm",
                },
            }
        );
        checkout.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await CreateAdminClient().DeleteAsync($"/api/products/{product.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
