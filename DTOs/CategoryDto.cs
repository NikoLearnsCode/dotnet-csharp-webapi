using System.ComponentModel.DataAnnotations;

namespace dotnet_backend_2.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string ImageUrl,
    string UrlSlug,
    List<ProductSummaryDto> Products
);

public record ProductSummaryDto(
    int Id,
    string Name,
    decimal Price,
    string UrlSlug
);

public class CreateCategoryDto
{
    [MinLength(1, ErrorMessage = "Category name cannot be empty.")]
    [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Image URL is required.")]
    [MinLength(1, ErrorMessage = "Image URL cannot be empty.")]
    public required string ImageUrl { get; set; }
}

public class UpdateCategoryDto
{
    [MinLength(1, ErrorMessage = "Category name cannot be empty.")]
    [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters.")]
    public string? Name { get; set; }

    [MinLength(1, ErrorMessage = "Image URL cannot be empty.")]
    public string? ImageUrl { get; set; }
}
