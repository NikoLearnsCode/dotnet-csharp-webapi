using Microsoft.EntityFrameworkCore;

namespace dotnet_backend_2.Data.Entities;

[Index(nameof(UrlSlug), IsUnique = true)]
public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
    public required string UrlSlug { get; set; }
    public ICollection<Product> Products { get; set; } = [];
}
