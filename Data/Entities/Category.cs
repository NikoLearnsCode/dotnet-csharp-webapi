using Microsoft.EntityFrameworkCore;

namespace dotnet_backend_2.Data.Entities;

[Index(nameof(UrlSlug), IsUnique = true)]
public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
    public required string UrlSlug { get; set; }

    /// <summary>Display order among siblings; lower comes first. Ties are ordered by name.</summary>
    public int SortOrder { get; set; }

    /// <summary>BRANCH (container for subcategories) or LEAF (holds products).</summary>
    public CategoryType Type { get; set; }

    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public List<Category> Children { get; set; } = new();

    public ICollection<Product> Products { get; set; } = [];
}
