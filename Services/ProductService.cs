using Microsoft.EntityFrameworkCore;
using dotnet_backend_2.DTOs;
using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.Data;
using dotnet_backend_2.Helpers;

namespace dotnet_backend_2.Services;

public class ProductService(ApplicationDbContext context) : IProductService
{
    // Page pagination
    public async Task<PagedList<ProductDto>> GetAllAsync(int pageNumber, int pageSize, bool includeCategories, string? slug, string? categorySlug, bool includeImages)
    {
        var query = context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(slug))
            query = query.Where(p => p.UrlSlug == slug);

        if (!string.IsNullOrEmpty(categorySlug))
            query = query.Where(p => p.Categories.Any(c => c.UrlSlug == categorySlug));

        if (includeCategories)
            query = query.Include(p => p.Categories);

        var totalCount = await query.CountAsync();

        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var productDtos = products.Select(p => MapToDto(p, includeCategories, includeImages)).ToList();

        return new PagedList<ProductDto>(productDtos, totalCount, pageNumber, pageSize);
    }

    // Cursor pagination
    public async Task<CursorPagedList<ProductDto>> GetAllCursorAsync(bool includeCategories, int limit, string? cursor, string? categorySlug, string? searchTerm)
    {
        var query = context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(categorySlug))
        {
            query = query.Where(p => p.Categories.Any(c => c.UrlSlug == categorySlug));
        }
        /*
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var lowerSearchTerm = searchTerm.ToLower();

                    query = query.Where(p => p.Name.ToLower().Contains(lowerSearchTerm) ||

                    p.Categories.Any(c => c.Name.ToLower().Contains(lowerSearchTerm))
                    );
                }
         */
        if (!string.IsNullOrEmpty(searchTerm))
        {

            var separators = new char[] { ' ', ',', '.', '-' };
            var searchWords = searchTerm.ToLower()
                .Split(separators, StringSplitOptions.RemoveEmptyEntries);

            if (searchWords.Length > 0)
            {
                query = query.Where(p =>
                    searchWords.All(word =>
                        p.Name.ToLower().Contains(word) ||

                        p.Categories.Any(c => c.Name.ToLower().Contains(word))
                )
                );
            }
        }

        if (!string.IsNullOrEmpty(cursor))
        {
            if (int.TryParse(cursor, out int cursorId))
            {
                query = query.Where(p => p.Id > cursorId);
            }

        }

        if (includeCategories)
            query = query.Include(p => p.Categories);

        query = query.OrderBy(p => p.Id);

        var products = await query.Take(limit + 1).ToListAsync();

        string? nextCursor = null;
        if (products.Count > limit)
        {
            var nextProduct = products[limit - 1];
            nextCursor = nextProduct.Id.ToString();
            products = [.. products.Take(limit)];
        }

        var productDtos = products.Select(p => MapToDto(p, includeCategories)).ToList();

        return new CursorPagedList<ProductDto>
        {
            Items = productDtos,
            NextCursor = nextCursor
        };
    }

    public async Task<ProductDto?> GetByIdAsync(int id, bool includeCategories)
    {
        var query = context.Products.AsQueryable();

        if (includeCategories)
            query = query.Include(p => p.Categories);

        var product = await query.FirstOrDefaultAsync(p => p.Id == id);
        return product is not null ? MapToDto(product, includeCategories) : null;
    }

    public async Task<ProductWithRelatedDto?> GetBySlugAsync(string slug, bool includeCategories)
    {
        var query = context.Products.AsQueryable();

        if (includeCategories)
            query = query.Include(p => p.Categories);

        var product = await query.FirstOrDefaultAsync(p => p.UrlSlug == slug);

        if (product is null)
            return null;

        // Load related products based on the product's categories.
        var categoryIds = product.Categories.Select(c => c.Id).ToList();

        var relatedProducts = await context.Products
            .Include(p => p.Categories)
            .Where(p => p.Id != product.Id && p.Categories.Any(c => categoryIds.Contains(c.Id)))
            .Take(8)
            .ToListAsync();

        var productDto = MapToDto(product, includeCategories);
        var relatedProductDtos = relatedProducts.Select(p => MapToDto(p, includeCategories)).ToList();

        return new ProductWithRelatedDto(productDto, relatedProductDtos);
    }


    public async Task<ProductDto> CreateAsync(CreateProductDto createDto)
    {
        var nameExists = await context.Products
            .AnyAsync(p => p.Name.ToLower() == createDto.Name.ToLower());
        if (nameExists)
            throw new InvalidOperationException($"A product named '{createDto.Name}' already exists.");

        if (createDto.CategoryIds == null || createDto.CategoryIds.Count == 0)
        {
            throw new InvalidOperationException("At least one category must be specified.");
        }

        var categories = await context.Categories
            .Where(c => createDto.CategoryIds.Contains(c.Id))
            .ToListAsync();

        if (categories.Count != createDto.CategoryIds.Count)
        {
            var foundCategoryIds = categories.Select(c => c.Id);
            var missingCategoryIds = createDto.CategoryIds.Except(foundCategoryIds);
            throw new InvalidOperationException($"The following category IDs were not found: {string.Join(", ", missingCategoryIds)}");
        }

        var product = new Product
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            ImageUrl = createDto.ImageUrl,
            UrlSlug = StringUtils.GenerateSlug(createDto.Name),
            Categories = categories
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();
        return MapToDto(product, true);
    }

    public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto)
    {
        var product = await context.Products
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return null;

        if (updateDto.Name is not null)
        {
            var nameExists = await context.Products
                .AnyAsync(p => p.Id != id && p.Name.ToLower() == updateDto.Name.ToLower());
            if (nameExists)
                throw new InvalidOperationException($"A product named '{updateDto.Name}' already exists.");

            product.Name = updateDto.Name;
            product.UrlSlug = StringUtils.GenerateSlug(updateDto.Name);
        }
        if (updateDto.Description is not null) product.Description = updateDto.Description;

        if (updateDto.Price.HasValue) product.Price = updateDto.Price.Value;

        if (updateDto.ImageUrl is not null) product.ImageUrl = updateDto.ImageUrl;

        if (updateDto.CategoryIds is not null)
        {

            if (updateDto.CategoryIds.Count == 0)
            {
                throw new InvalidOperationException("A product must belong to at least one category. To keep categories unchanged, omit 'categoryIds' from the request.");
            }

            var categories = await context.Categories
                .Where(c => updateDto.CategoryIds.Contains(c.Id))
                .ToListAsync();

            if (categories.Count != updateDto.CategoryIds.Count)
            {
                var foundCategoryIds = categories.Select(c => c.Id);
                var missingCategoryIds = updateDto.CategoryIds.Except(foundCategoryIds);

                throw new InvalidOperationException($"The following category IDs do not exist: {string.Join(", ", missingCategoryIds)}");
            }

            product.Categories = categories;
        }

        await context.SaveChangesAsync();
        return MapToDto(product, true);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return false;

        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return true;
    }

    private static ProductDto MapToDto(Product product, bool includeCategories, bool includeImages = true) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        includeImages ? product.ImageUrl : string.Empty,
        product.UrlSlug,
        includeCategories
            ? [.. product.Categories.Select(c => new CategorySummaryDto(
                c.Id,
                c.Name,
                c.UrlSlug
            ))]
            : []
    );
}
