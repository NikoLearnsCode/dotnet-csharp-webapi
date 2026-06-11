using dotnet_backend_2.DTOs;

namespace dotnet_backend_2.Services;

public interface ICategoryService
{
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<List<CategoryTreeDto>> GetCategoryTreeAsync();

    Task<CategoryDto> CreateAsync(CreateCategoryDto createDto);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto updateDto);
    Task<bool> DeleteAsync(int id);
}
