using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(bool includeProducts, string? slug, int maxProducts);
    Task<CategoryDto?> GetByIdAsync(int id, bool includeProducts, int maxProducts);
    
    Task<CategoryDto> CreateAsync(CreateCategoryDto createDto);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto updateDto);
    Task<bool> DeleteAsync(int id);
}
