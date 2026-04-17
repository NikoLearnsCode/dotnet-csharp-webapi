using Microsoft.EntityFrameworkCore;
using dotnet_backend_2.Data.Entities;
using dotnet_backend_2.Helpers;

namespace dotnet_backend_2.Data;

public static class DataSeeder
{

    private const int PRODUCT_MULTIPLIER = 100;
    // Base dataset size is 5 products times PRODUCT_MULTIPLIER.

    public static void SeedAll(ModelBuilder modelBuilder)
    {
        SeedUsers(modelBuilder);
        SeedCategories(modelBuilder);
        SeedProducts(modelBuilder);
        SeedProductCategoryRelations(modelBuilder);
    }

    // Test/demo only — SeedUsers adds a fixed admin for local setup (@ README.md).
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

    private static void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = 1,
                Name = "Clothes",
                ImageUrl = "/images/categories/klader.jpg",
                UrlSlug = "clothes"
            },
            new Category
            {
                Id = 2,
                Name = "Shoes",
                ImageUrl = "/images/categories/skor.jpg",
                UrlSlug = "shoes"
            },
            new Category
            {
                Id = 3,
                Name = "Accessories",
                ImageUrl = "/images/categories/accessoarer.jpg",
                UrlSlug = "accessoarer"
            }
        );
    }

    private static void SeedProducts(ModelBuilder modelBuilder)
    {
        var baseProducts = new[]
        {
            new { Name = "Black T-shirt", Description = "A comfortable black cotton t-shirt", Price = 199.99m, ImageUrl = "/images/products/svart-tshirt.jpg", CategoryId = 1 },

            new { Name = "Blue Jeans", Description = "Classic slim-fit blue jeans", Price = 699.99m, ImageUrl = "/images/products/bla-jeans.jpg", CategoryId = 1 },

            new { Name = "White Sneakers", Description = "Stylish white sneakers for everyday wear", Price = 899.99m, ImageUrl = "/images/products/vita-sneakers.jpg", CategoryId = 2 },

            new { Name = "Leather Handbag", Description = "Elegant handbag made from genuine leather", Price = 1299.99m, ImageUrl = "/images/products/lader-handvaska.jpg", CategoryId = 3 },

            new { Name = "Cotton Hoodie", Description = "Cozy hoodie in soft cotton", Price = 499.99m, ImageUrl = "/images/products/bomulls-hoodie.jpg", CategoryId = 1 }
        };

        var products = new List<Product>();
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

                productId++;
            }
        }

        modelBuilder.Entity<Product>().HasData(products);
    }

    private static void SeedProductCategoryRelations(ModelBuilder modelBuilder)
    {
        var baseRelations = new[]
        {
            new { ProductIndex = 0, CategoryId = 1 },
            new { ProductIndex = 1, CategoryId = 1 },
            new { ProductIndex = 2, CategoryId = 2 },
            new { ProductIndex = 3, CategoryId = 3 },
            new { ProductIndex = 4, CategoryId = 1 }
        };

        var relations = new List<object>();
        var baseProductCount = 5;

        for (int multiplier = 0; multiplier < PRODUCT_MULTIPLIER; multiplier++)
        {
            foreach (var relation in baseRelations)
            {
                var productId = (multiplier * baseProductCount) + relation.ProductIndex + 1;
                relations.Add(new { CategoriesId = relation.CategoryId, ProductsId = productId });
            }
        }

        modelBuilder.Entity("CategoryProduct").HasData([.. relations]);
    }
}

