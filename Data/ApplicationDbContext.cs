
using Microsoft.EntityFrameworkCore;
using dotnet_backend_2.Data.Entities;

namespace dotnet_backend_2.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Cart> Carts {get; set;}
    public DbSet<CartItem> CartItems {get; set;}
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Price)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            entity.HasIndex(e => e.SessionId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);
        });

        DataSeeder.SeedAll(modelBuilder);
    }
}



