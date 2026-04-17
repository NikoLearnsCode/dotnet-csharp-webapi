using Microsoft.EntityFrameworkCore;
using dotnet_backend_2.DTOs;
using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.Data;
using dotnet_backend_2.Helpers;

namespace dotnet_backend_2.Services;

public class CategoryService(ApplicationDbContext context) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(bool includeProducts, string? slug, int maxProducts)
    {
        var query = context.Categories.AsQueryable();

        if (!string.IsNullOrEmpty(slug))
            query = query.Where(c => c.UrlSlug == slug);

        if (includeProducts)
            query = query.Include(c => c.Products.OrderByDescending(p => p.Id).Take(maxProducts));

        var categories = await query.ToListAsync();
        return [.. categories.Select(c => MapToDto(c, includeProducts))];
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, bool includeProducts, int maxProducts)
    {
        var query = context.Categories.AsQueryable();

        if (includeProducts)
            query = query.Include(c => c.Products.OrderByDescending(p => p.Id).Take(maxProducts));

        var category = await query.FirstOrDefaultAsync(c => c.Id == id);
        return category is not null ? MapToDto(category, includeProducts) : null;
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto createDto)
    {
        var nameExists = await context.Categories
            .AnyAsync(c => c.Name.ToLower() == createDto.Name.ToLower());
        if (nameExists)
            throw new InvalidOperationException($"A category named '{createDto.Name}' already exists.");

        var category = new Category
        {
            Name = createDto.Name,
            ImageUrl = createDto.ImageUrl,
            UrlSlug = StringUtils.GenerateSlug(createDto.Name)
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return MapToDto(category, false);
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto updateDto)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null) return null;

        if (updateDto.Name is not null)
        {
            var nameExists = await context.Categories
                .AnyAsync(c => c.Id != id && c.Name.ToLower() == updateDto.Name.ToLower());
            if (nameExists)
                throw new InvalidOperationException($"A category named '{updateDto.Name}' already exists.");

            category.Name = updateDto.Name;
            category.UrlSlug = StringUtils.GenerateSlug(updateDto.Name);
        }
        if (updateDto.ImageUrl is not null) category.ImageUrl = updateDto.ImageUrl;

        await context.SaveChangesAsync();
        return MapToDto(category, false);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null) return false;

        var hasProducts = await context.Products
            .AnyAsync(p => p.Categories.Any(c => c.Id == id));

        if (hasProducts)
        {
            throw new InvalidOperationException("Cannot delete a category that still has associated products.");
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return true;
    }


    private static CategoryDto MapToDto(Category category, bool includeProducts) => new(
        category.Id,
        category.Name,
        category.ImageUrl,
        category.UrlSlug,
        includeProducts
            ? [.. category.Products.Select(p => new ProductSummaryDto(
                p.Id,
                p.Name,
                p.Price,
                p.UrlSlug
            ))]
            : []
    );
}