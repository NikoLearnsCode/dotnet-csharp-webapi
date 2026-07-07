using System.ComponentModel.DataAnnotations;

namespace WebApi.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string ImageUrl,
    string UrlSlug,
    int? ParentId,
    int SortOrder,
    string Type
);

public record CategoryTreeDto(
    int Id,
    string Name,
    string ImageUrl,
    string UrlSlug,
    int? ParentId,
    int SortOrder,
    string Type,
    List<CategoryTreeDto> Children
);

public class CreateCategoryDto
{
    [MinLength(1, ErrorMessage = "Category name cannot be empty.")]
    [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Image URL is required.")]
    [MinLength(1, ErrorMessage = "Image URL cannot be empty.")]
    public required string ImageUrl { get; set; }

    /// <summary>BRANCH (container for subcategories) or LEAF (holds products). Chosen explicitly at creation.</summary>
    [Required(ErrorMessage = "Category type is required: BRANCH or LEAF.")]
    public required string Type { get; set; }

    /// <summary>Optional parent category (must be a BRANCH). Omit or null to create a root category.</summary>
    public int? ParentId { get; set; }

    /// <summary>Display order among siblings; lower comes first. Omit for default (0).</summary>
    public int SortOrder { get; set; }
}

public class UpdateCategoryDto
{
    [MinLength(1, ErrorMessage = "Category name cannot be empty.")]
    [StringLength(100, ErrorMessage = "Category name cannot be longer than 100 characters.")]
    public string? Name { get; set; }

    [MinLength(1, ErrorMessage = "Image URL cannot be empty.")]
    public string? ImageUrl { get; set; }

    /// <summary>Change the node type. To LEAF requires no subcategories; to BRANCH requires no products.</summary>
    public string? Type { get; set; }

    /// <summary>Move the category under a new parent (must be a BRANCH). Omit to leave the parent unchanged.</summary>
    public int? ParentId { get; set; }

    /// <summary>Set to true to make the category a root category. Cannot be combined with ParentId.</summary>
    public bool MoveToRoot { get; set; }

    /// <summary>Display order among siblings; lower comes first. Omit to leave unchanged.</summary>
    public int? SortOrder { get; set; }
}
