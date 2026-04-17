using System.ComponentModel.DataAnnotations;

namespace dotnet_backend_2.DTOs;

public record ProductDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string ImageUrl,
    string UrlSlug,
    List<CategorySummaryDto>? Category
);

public record CategorySummaryDto(
    int Id,
    string Name,
    string UrlSlug
);

public class CreateProductDto
{
    [Required(ErrorMessage = "Product name is required.")]
    [MinLength(1, ErrorMessage = "Product name cannot be empty.")]
    [StringLength(100, ErrorMessage = "Product name cannot be longer than 100 characters.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [MinLength(1, ErrorMessage = "Description cannot be empty.")]
    public required string Description { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Image URL is required.")]
    [MinLength(1, ErrorMessage = "Image URL cannot be empty.")]
    public required string ImageUrl { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "A product must belong to at least one category.")]
    public List<int> CategoryIds { get; set; } = [];
}

public class UpdateProductDto
{
    [MinLength(1, ErrorMessage = "Product name cannot be empty.")]
    [StringLength(100, ErrorMessage = "Product name cannot be longer than 100 characters.")]
    public string? Name { get; set; }

    [MinLength(1, ErrorMessage = "Description cannot be empty.")]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal? Price { get; set; }

    [MinLength(1, ErrorMessage = "Image URL cannot be empty.")]
    public string? ImageUrl { get; set; }

    [MinLength(1, ErrorMessage = "A product must belong to at least one category.")]
    public List<int>? CategoryIds { get; set; }
}

public record ProductWithRelatedDto(
    ProductDto Product,
    List<ProductDto> RelatedProducts
);