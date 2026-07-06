using WebApi.DTOs;

namespace WebApi.Services;

public interface ICategoryService
{
    Task<CategoryDto?> GetByIdAsync(int id);
    Task<List<CategoryTreeDto>> GetCategoryTreeAsync();

    Task<CategoryDto> CreateAsync(CreateCategoryDto createDto);
    Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto updateDto);
    Task<bool> DeleteAsync(int id);
}
