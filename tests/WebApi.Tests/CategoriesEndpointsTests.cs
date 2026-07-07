using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using WebApi.DTOs;
using WebApi.Tests.Infrastructure;

namespace WebApi.Tests;

public class CategoriesEndpointsTests(CustomWebApplicationFactory factory)
    : IntegrationTestBase(factory)
{
    private static CreateCategoryDto NewCategory(string name, string type, int? parentId = null) =>
        new()
        {
            Name = name,
            ImageUrl = "/images/categories/test.jpg",
            Type = type,
            ParentId = parentId,
        };

    private async Task<CategoryDto> CreateCategoryAsync(
        string name,
        string type,
        int? parentId = null
    )
    {
        var response = await CreateAdminClient()
            .PostAsJsonAsync("/api/categories", NewCategory(name, type, parentId));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    [Fact]
    public async Task GetCategoryTree_ReturnsNonEmptyTree()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/categories/tree");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetCategoryById_Existing_ReturnsOk()
    {
        var client = CreateClient();
        var branchId = await GetBranchCategoryIdAsync();

        var response = await client.GetAsync($"/api/categories/{branchId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCategory_Anonymous_ReturnsUnauthorized()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/categories",
            NewCategory("Should Not Be Created", "LEAF")
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_LeafUnderBranch_ReturnsCreated()
    {
        var branchId = await GetBranchCategoryIdAsync();

        var created = await CreateCategoryAsync("Test Leaf Under Branch", "LEAF", branchId);

        created.ParentId.ShouldBe(branchId);
        created.Type.ShouldBe("LEAF");
    }

    [Fact]
    public async Task CreateCategory_UnderLeaf_ReturnsBadRequest()
    {
        var leafId = await GetLeafCategoryIdAsync();

        var response = await CreateAdminClient()
            .PostAsJsonAsync("/api/categories", NewCategory("Nested Under Leaf", "LEAF", leafId));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteCategory_WithSubcategories_ReturnsConflict()
    {
        // Seeded branch roots always have children.
        var branchId = await GetBranchCategoryIdAsync();

        var response = await CreateAdminClient().DeleteAsync($"/api/categories/{branchId}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteCategory_EmptyLeaf_ReturnsNoContent()
    {
        var branchId = await GetBranchCategoryIdAsync();
        var leaf = await CreateCategoryAsync("Disposable Leaf", "LEAF", branchId);

        var response = await CreateAdminClient().DeleteAsync($"/api/categories/{leaf.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MoveCategory_IntoOwnDescendant_ReturnsBadRequest()
    {
        var parent = await CreateCategoryAsync("Cycle Parent", "BRANCH");
        var child = await CreateCategoryAsync("Cycle Child", "BRANCH", parent.Id);

        var response = await CreateAdminClient()
            .PatchAsJsonAsync(
                $"/api/categories/{parent.Id}",
                new UpdateCategoryDto { ParentId = child.Id }
            );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
