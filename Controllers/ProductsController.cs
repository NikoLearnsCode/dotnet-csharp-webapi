using Microsoft.AspNetCore.Mvc;
using dotnet_backend_2.DTOs;
using dotnet_backend_2.Services;
using Microsoft.AspNetCore.Authorization;

namespace dotnet_backend_2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    // Page pagination
    private static readonly List<int> AllowedPageSizes = [10, 25, 50, 100];
    private const int DefaultPageSize = 10;
    [HttpGet]
    public async Task<ActionResult<PagedList<ProductDto>>> GetProducts(
        bool includeCategories = true,
        string? slug = null,
        string? categorySlug = null,
        int page = 1,
        int pageSize = DefaultPageSize,
        bool includeImages = true)
    {
        if (!AllowedPageSizes.Contains(pageSize))
        {
            pageSize = DefaultPageSize;
        }

        var products = await productService.GetAllAsync(page, pageSize, includeCategories, slug, categorySlug, includeImages);

        return Ok(products);
    }

    // Cursor pagination
    private const int MaxCursorPageSize = 30;
    private const int DefaultCursorPageSize = 12;
    [HttpGet("cursor")]
    public async Task<ActionResult<CursorPagedList<ProductDto>>> GetProductsCursor(
    bool includeCategories = true,
    int limit = DefaultCursorPageSize,
    string? cursor = null,
    string? categorySlug = null,
    string? searchTerm = null
    )
    {
        if (limit > MaxCursorPageSize)
        {
            limit = MaxCursorPageSize;
        }

        var products = await productService.GetAllCursorAsync(includeCategories, limit, cursor, categorySlug, searchTerm);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id, bool includeCategories = true)
    {
        var product = await productService.GetByIdAsync(id, includeCategories);

        if (product == null)
        {
            return NotFound($"Product with ID {id} not found.");
        }

        return Ok(product);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductWithRelatedDto>> GetProductBySlug(string slug, bool includeCategories = true)
    {
        var result = await productService.GetBySlugAsync(slug, includeCategories);

        if (result == null)
        {
            return NotFound($"Product with slug '{slug}' not found.");
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto createDto)
    {
        try
        {
            var product = await productService.CreateAsync(createDto);

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);

        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto updateDto)
    {
        try
        {
            var product = await productService.UpdateAsync(id, updateDto);

            if (product is null)
                return NotFound($"Product with ID {id} not found.");

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var success = await productService.DeleteAsync(id);

        if (!success)
            return NotFound($"Product with ID {id} not found.");

        return NoContent();
    }
}
