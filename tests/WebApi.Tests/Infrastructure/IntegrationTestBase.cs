using System.Net.Http.Json;
using WebApi.DTOs;

namespace WebApi.Tests.Infrastructure;

/// <summary>
/// Base for endpoint tests. <see cref="IClassFixture{T}"/> gives each test class its
/// own factory instance - and therefore its own freshly seeded database -
/// while tests within a class share it (and run sequentially by xUnit default).
/// </summary>
public abstract class IntegrationTestBase(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly CustomWebApplicationFactory Factory = factory;

    /// Anonymous client (no auth headers) - [Authorize] endpoints return 401.
    protected HttpClient CreateClient() => Factory.CreateClient();

    protected HttpClient CreateAdminClient()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");
        return client;
    }

    protected HttpClient CreateUserClient(int userId = 1)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "User");
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }

    // Seed lookups go through the API instead of hardcoding ids from SeedData,
    // so reshuffling the seed doesn't silently break the tests.

    protected async Task<int> GetLeafCategoryIdAsync() => (await FindCategoryAsync("LEAF")).Id;

    protected async Task<int> GetBranchCategoryIdAsync() => (await FindCategoryAsync("BRANCH")).Id;

    protected async Task<CategoryTreeDto> FindCategoryAsync(string type)
    {
        var tree = await CreateClient()
            .GetFromJsonAsync<List<CategoryTreeDto>>("/api/categories/tree");
        return Flatten(tree!).FirstOrDefault(c => c.Type == type)
            ?? throw new InvalidOperationException($"No seeded category of type {type} found.");
    }

    private static IEnumerable<CategoryTreeDto> Flatten(IEnumerable<CategoryTreeDto> nodes) =>
        nodes.SelectMany(n => new[] { n }.Concat(Flatten(n.Children)));

    /// Any seeded product; index picks distinct products when a test needs several.
    protected async Task<ProductDto> GetSeededProductAsync(int index = 0)
    {
        var page = await CreateClient().GetFromJsonAsync<PagedProducts>("/api/products");
        return page!.Items[index];
    }
}

// Minimal shapes for deserializing responses whose production DTOs can't round-trip
// through System.Text.Json (e.g. PagedList<T> exposes only a constructor whose
// parameter names don't match the emitted JSON).
public record LoginResult(string Username, string Role, int UserId, int CartItemsMerged);

public record PagedProducts(
    List<ProductDto> Items,
    int CurrentPage,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
);

// OrderResponseDto emits totalAmount/unitPrice/lineTotal from get-only computed
// properties, so it can't be deserialized back - these shapes mirror the JSON.
public record OrderResult(
    int Id,
    string ConfirmationToken,
    decimal TotalAmount,
    string Status,
    string Email,
    List<OrderItemResult> OrderItems
);

public record OrderItemResult(
    int Id,
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
