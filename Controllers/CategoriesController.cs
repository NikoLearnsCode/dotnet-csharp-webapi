using Microsoft.AspNetCore.Mvc;
using dotnet_backend_2.DTOs;
using dotnet_backend_2.Services;
using Microsoft.AspNetCore.Authorization;

namespace dotnet_backend_2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet("tree")]
    public async Task<ActionResult<List<CategoryTreeDto>>> GetCategoryTree()
    {
        var tree = await categoryService.GetCategoryTreeAsync();
        return Ok(tree);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await categoryService.GetByIdAsync(id);

        if (category == null)
        {
            return Problem(detail: $"Category with ID {id} not found.", statusCode: StatusCodes.Status404NotFound);
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
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
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
                return Problem(detail: $"Category with ID {id} not found.", statusCode: StatusCodes.Status404NotFound);

            return Ok(category);

        }
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
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
                return Problem(detail: $"Category with ID {id} not found.", statusCode: StatusCodes.Status404NotFound);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            // The category exists but its current state (subcategories/products) blocks deletion.
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
