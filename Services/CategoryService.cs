using Microsoft.EntityFrameworkCore;
using WebApi.Data;
using WebApi.Data.Entities;
using WebApi.DTOs;
using WebApi.Helpers;

namespace WebApi.Services;

public class CategoryService(ApplicationDbContext context) : ICategoryService
{
    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        return category is not null ? MapToDto(category) : null;
    }

    public async Task<List<CategoryTreeDto>> GetCategoryTreeAsync()
    {
        // One flat query - no Include, AsNoTracking for read-only access.
        var allCategories = await context.Categories.AsNoTracking().ToListAsync();

        // Group children by ParentId
        var childrenByParentId = allCategories.ToLookup(c => c.ParentId);

        // Build the tree top-down, starting from the roots (ParentId == null).
        // Siblings are ordered by SortOrder, with name as tiebreaker.
        List<CategoryTreeDto> BuildSubtree(int? parentId) =>
            [
                .. childrenByParentId[parentId]
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => new CategoryTreeDto(
                        c.Id,
                        c.Name,
                        c.ImageUrl,
                        c.UrlSlug,
                        c.ParentId,
                        c.SortOrder,
                        TypeName(c.Type),
                        BuildSubtree(c.Id)
                    )),
            ];

        return BuildSubtree(null);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto createDto)
    {
        var nameExists = await context.Categories.AnyAsync(c =>
            c.Name.ToLower() == createDto.Name.ToLower()
        );
        if (nameExists)
            throw new InvalidOperationException(
                $"A category named '{createDto.Name}' already exists."
            );

        var type = ParseType(createDto.Type);

        if (createDto.ParentId is not null)
        {
            var parent = await context.Categories.FirstOrDefaultAsync(c =>
                c.Id == createDto.ParentId
            );
            if (parent is null)
                throw new InvalidOperationException(
                    $"Parent category with ID {createDto.ParentId} does not exist."
                );

            if (parent.Type != CategoryType.Branch)
                throw new InvalidOperationException(
                    $"Cannot create a category under '{parent.Name}' because it is a LEAF category. Subcategories can only be placed under BRANCH categories."
                );
        }

        var category = new Category
        {
            Name = createDto.Name,
            ImageUrl = createDto.ImageUrl,
            UrlSlug = StringUtils.GenerateSlug(createDto.Name),
            Type = type,
            ParentId = createDto.ParentId,
            SortOrder = createDto.SortOrder,
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return MapToDto(category);
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto updateDto)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return null;

        if (updateDto.Name is not null)
        {
            var nameExists = await context.Categories.AnyAsync(c =>
                c.Id != id && c.Name.ToLower() == updateDto.Name.ToLower()
            );
            if (nameExists)
                throw new InvalidOperationException(
                    $"A category named '{updateDto.Name}' already exists."
                );

            category.Name = updateDto.Name;
            category.UrlSlug = StringUtils.GenerateSlug(updateDto.Name);
        }
        if (updateDto.ImageUrl is not null)
            category.ImageUrl = updateDto.ImageUrl;

        if (updateDto.SortOrder is not null)
            category.SortOrder = updateDto.SortOrder.Value;

        if (updateDto.Type is not null)
        {
            var newType = ParseType(updateDto.Type);
            if (newType != category.Type)
            {
                if (
                    newType == CategoryType.Leaf
                    && await context.Categories.AnyAsync(c => c.ParentId == id)
                )
                    throw new InvalidOperationException(
                        $"Cannot change '{category.Name}' to LEAF while it has subcategories. Move or delete them first."
                    );

                if (
                    newType == CategoryType.Branch
                    && await context.Products.AnyAsync(p => p.Categories.Any(c => c.Id == id))
                )
                    throw new InvalidOperationException(
                        $"Cannot change '{category.Name}' to BRANCH while it has products. Move the products first."
                    );

                category.Type = newType;
            }
        }

        if (updateDto.MoveToRoot && updateDto.ParentId is not null)
            throw new InvalidOperationException("Cannot set both MoveToRoot and ParentId.");

        if (updateDto.MoveToRoot)
        {
            category.ParentId = null;
        }
        else if (updateDto.ParentId is not null)
        {
            await MoveUnderParentAsync(category, updateDto.ParentId.Value);
        }

        await context.SaveChangesAsync();
        return MapToDto(category);
    }

    // Moves a category under a new parent, rejecting cycles (a category cannot become a child of itself or of one of its own descendants).
    private async Task MoveUnderParentAsync(Category category, int newParentId)
    {
        if (newParentId == category.Id)
            throw new InvalidOperationException("A category cannot be its own parent.");

        var allCategories = await context
            .Categories.AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.ParentId,
                c.Name,
                c.Type,
            })
            .ToListAsync();

        var newParent = allCategories.FirstOrDefault(c => c.Id == newParentId);
        if (newParent is null)
            throw new InvalidOperationException(
                $"Parent category with ID {newParentId} does not exist."
            );

        if (newParent.Type != CategoryType.Branch)
            throw new InvalidOperationException(
                $"Cannot move a category under '{newParent.Name}' because it is a LEAF category. Subcategories can only be placed under BRANCH categories."
            );

        // Walk up from the new parent; reaching this category means it is one of its own descendants
        var parentById = allCategories.ToDictionary(c => c.Id, c => c.ParentId);
        int? cursor = newParentId;
        var steps = 0;
        while (cursor is not null)
        {
            if (cursor == category.Id)
                throw new InvalidOperationException(
                    "Cannot move a category into one of its own descendants."
                );
            if (++steps > parentById.Count)
                throw new InvalidOperationException(
                    "Category hierarchy contains a cycle; fix the data before moving categories."
                );
            cursor = parentById[cursor.Value];
        }

        category.ParentId = newParentId;
    }

    private static CategoryType ParseType(string value)
    {
        if (!Enum.TryParse<CategoryType>(value, ignoreCase: true, out var type))
            throw new InvalidOperationException(
                $"Invalid category type '{value}'. Valid values: BRANCH, LEAF."
            );
        return type;
    }

    private static string TypeName(CategoryType type) => type.ToString().ToUpperInvariant();

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await context.Categories.FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return false;

        var hasChildren = await context.Categories.AnyAsync(c => c.ParentId == id);
        if (hasChildren)
        {
            throw new InvalidOperationException(
                "Cannot delete a category that has subcategories. Delete or move them first."
            );
        }

        var hasProducts = await context.Products.AnyAsync(p => p.Categories.Any(c => c.Id == id));

        if (hasProducts)
        {
            throw new InvalidOperationException(
                "Cannot delete a category that still has associated products."
            );
        }

        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return true;
    }

    private static CategoryDto MapToDto(Category category) =>
        new(
            category.Id,
            category.Name,
            category.ImageUrl,
            category.UrlSlug,
            category.ParentId,
            category.SortOrder,
            TypeName(category.Type)
        );
}
