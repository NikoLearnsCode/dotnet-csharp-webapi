using Microsoft.EntityFrameworkCore;
using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.Helpers;

namespace dotnet_backend_2.Data;

public static class DataSeeder
{
    private const int PRODUCT_MULTIPLIER = 100;
    // Base dataset size is 5 products times PRODUCT_MULTIPLIER.

    // Stable category ids referenced from products and from the category tree below.
    // Using named constants instead of bare numbers keeps the relations readable and
    // makes it obvious what a product is attached to.
    private static class CategoryIds
    {
        // Roots
        public const int Clothes = 1;
        public const int Shoes = 2;
        public const int Accessories = 3;
        // Level 1
        public const int Tops = 4;        // under Clothes
        public const int Bottoms = 5;     // under Clothes
        public const int Sneakers = 6;    // under Shoes
        public const int Boots = 7;       // under Shoes
        public const int Bags = 8;        // under Accessories
        // Level 2
        public const int TShirts = 9;     // under Tops
        public const int Hoodies = 10;    // under Tops
        public const int Jeans = 11;      // under Bottoms
    }

    public static void SeedAll(ModelBuilder modelBuilder)
    {
        SeedUsers(modelBuilder);
        SeedCategories(modelBuilder);
        SeedProductsWithRelations(modelBuilder);
    }

    // Test/demo only - SeedUsers adds a fixed admin for local setup (@ README.md).
    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        const string adminPasswordHash = "$2a$11$Dwjmx1HTPGBHygbMwcBGpuq4AVknT0PzpfLDVzhcEIJRImpx8dJbG";

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = adminPasswordHash,
                Role = "Admin"
            });
    }

    // The category tree. To add a category: give it a new id in CategoryIds, then add a
    // row here with its ParentId (null for a root). The hierarchy lives in this one place.
    private static void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            // Roots
            Branch(CategoryIds.Clothes, "Clothes", "clothes", parentId: null, sortOrder: 1),
            Branch(CategoryIds.Shoes, "Shoes", "shoes", parentId: null, sortOrder: 2),
            Branch(CategoryIds.Accessories, "Accessories", "accessories", parentId: null, sortOrder: 3),

            // Level 1
            Branch(CategoryIds.Tops, "Tops", "tops", parentId: CategoryIds.Clothes, sortOrder: 1),
            Branch(CategoryIds.Bottoms, "Bottoms", "bottoms", parentId: CategoryIds.Clothes, sortOrder: 2),
            Leaf(CategoryIds.Sneakers, "Sneakers", "sneakers", parentId: CategoryIds.Shoes, sortOrder: 1),
            Leaf(CategoryIds.Boots, "Boots", "boots", parentId: CategoryIds.Shoes, sortOrder: 2),
            Leaf(CategoryIds.Bags, "Bags", "bags", parentId: CategoryIds.Accessories, sortOrder: 1),

            // Level 2
            Leaf(CategoryIds.TShirts, "T-shirts", "t-shirts", parentId: CategoryIds.Tops, sortOrder: 1),
            Leaf(CategoryIds.Hoodies, "Hoodies", "hoodies", parentId: CategoryIds.Tops, sortOrder: 2),
            Leaf(CategoryIds.Jeans, "Jeans", "jeans", parentId: CategoryIds.Bottoms, sortOrder: 1)
        );
    }

    private static Category Branch(int id, string name, string slug, int? parentId, int sortOrder) =>
        Category(id, name, slug, parentId, sortOrder, CategoryType.Branch);

    private static Category Leaf(int id, string name, string slug, int? parentId, int sortOrder) =>
        Category(id, name, slug, parentId, sortOrder, CategoryType.Leaf);

    private static Category Category(int id, string name, string slug, int? parentId, int sortOrder, CategoryType type) => new()
    {
        Id = id,
        Name = name,
        UrlSlug = slug,
        ImageUrl = $"/images/categories/{slug}.jpg",
        ParentId = parentId,
        SortOrder = sortOrder,
        Type = type
    };

    // Products and their category links are generated together from a single definition,
    // so they can never drift apart. To add a product: add one entry to baseProducts with
    // the CategoryId it belongs to - the join row is derived automatically.
    private static void SeedProductsWithRelations(ModelBuilder modelBuilder)
    {
        var baseProducts = new[]
        {
            new { Name = "Black T-shirt",   Description = "A comfortable black cotton t-shirt",        Price = 199.99m,  ImageUrl = "/images/products/svart-tshirt.jpg",    CategoryId = CategoryIds.TShirts },
            new { Name = "Blue Jeans",      Description = "Classic slim-fit blue jeans",               Price = 699.99m,  ImageUrl = "/images/products/bla-jeans.jpg",       CategoryId = CategoryIds.Jeans },
            new { Name = "White Sneakers",  Description = "Stylish white sneakers for everyday wear",   Price = 899.99m,  ImageUrl = "/images/products/vita-sneakers.jpg",   CategoryId = CategoryIds.Sneakers },
            new { Name = "Leather Handbag", Description = "Elegant handbag made from genuine leather",  Price = 1299.99m, ImageUrl = "/images/products/lader-handvaska.jpg", CategoryId = CategoryIds.Bags },
            new { Name = "Cotton Hoodie",   Description = "Cozy hoodie in soft cotton",                Price = 499.99m,  ImageUrl = "/images/products/bomulls-hoodie.jpg",  CategoryId = CategoryIds.Hoodies }
        };

        var products = new List<Product>();
        var relations = new List<object>();
        var productId = 1;

        for (int multiplier = 0; multiplier < PRODUCT_MULTIPLIER; multiplier++)
        {
            foreach (var baseProduct in baseProducts)
            {
                var suffix = multiplier > 0 ? $" #{multiplier + 1}" : "";
                var slugSuffix = multiplier > 0 ? $"-{multiplier + 1}" : "";

                products.Add(new Product
                {
                    Id = productId,
                    Name = baseProduct.Name + suffix,
                    Description = baseProduct.Description,
                    Price = baseProduct.Price + (multiplier * 10),
                    ImageUrl = baseProduct.ImageUrl,
                    UrlSlug = StringUtils.GenerateSlug(baseProduct.Name) + slugSuffix
                });

                // Single source of truth: the join row reuses the product's own CategoryId.
                relations.Add(new { CategoriesId = baseProduct.CategoryId, ProductsId = productId });

                productId++;
            }
        }

        modelBuilder.Entity<Product>().HasData(products);
        modelBuilder.Entity("CategoryProduct").HasData([.. relations]);
    }
}
