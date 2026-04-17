using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Services;

public interface IProductService
{
    Task<PagedList<ProductDto>> GetAllAsync(int pageNumber, int pageSize, bool includeCategories, string? slug, string? categorySlug, bool includeImages);
    Task<CursorPagedList<ProductDto>> GetAllCursorAsync(bool includeCategories, int limit, string? cursor, string? categorySlug, string? searchTerm);
    Task<ProductDto?> GetByIdAsync(int id, bool includeCategories);
    Task<ProductWithRelatedDto?> GetBySlugAsync(string slug, bool includeCategories);

    Task<ProductDto> CreateAsync(CreateProductDto createDto);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto);
    Task<bool> DeleteAsync(int id);
}
