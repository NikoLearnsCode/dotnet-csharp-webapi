using Microsoft.AspNetCore.Mvc;
using dotnet_backend_2.DTOs;
using dotnet_backend_2.Services;
using Microsoft.AspNetCore.Authorization;

namespace dotnet_backend_2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    private const int MaxProductsLimit = 50;
    private const int DefaultMaxProducts = 10;

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories(
        bool includeProducts = false,
        string? slug = null,
        int maxProducts = DefaultMaxProducts)
    {
        if (maxProducts > MaxProductsLimit)
        {
            maxProducts = MaxProductsLimit;
        }

        var categories = await categoryService.GetAllAsync(includeProducts, slug, maxProducts);
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(
        int id,
        bool includeProducts = true,
        int maxProducts = DefaultMaxProducts)
    {
        if (maxProducts > MaxProductsLimit)
        {
            maxProducts = MaxProductsLimit;
        }

        var category = await categoryService.GetByIdAsync(id, includeProducts, maxProducts);

        if (category == null)
        {
            return NotFound($"Category with ID {id} not found.");
        }

        return Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> CreateCategory(CreateCategoryDto createDto)
    {
        try
        {
            var category = await categoryService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryDto updateDto)
    {
        try
        {
            var category = await categoryService.UpdateAsync(id, updateDto);

            if (category is null)
                return NotFound($"Category with ID {id} not found.");

            return Ok(category);

        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var success = await categoryService.DeleteAsync(id);

            if (!success)
                return NotFound($"Category with ID {id} not found.");

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
