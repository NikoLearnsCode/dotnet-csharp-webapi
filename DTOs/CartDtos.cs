using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dotnet_backend_2.DTOs;

public class CartDto
{
    public int Id { get; set; }
    public List<CartItemDto> Items { get; set; } = [];
    public int TotalItems { get; set; }

    [JsonIgnore]
    public decimal SubTotal { get; set; }

    [JsonPropertyName("subTotal")]
    public decimal SubTotalFormatted => Math.Round(SubTotal, 2);
}

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    // Flattened fields from Product
    public string ProductName { get; set; } = string.Empty;

    public string ProductUrlSlug { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal ProductPrice { get; set; }

    [JsonPropertyName("productPrice")]
    public decimal ProductPriceFormatted => Math.Round(ProductPrice, 2);

    public string? ProductImageUrl { get; set; }

    // Data from CartItem
    public int Quantity { get; set; }

    // (ProductPrice * Quantity)
    [JsonIgnore]
    public decimal LineTotal { get; set; }

    [JsonPropertyName("lineTotal")]
    public decimal LineTotalFormatted => Math.Round(LineTotal, 2);
}

public class AddToCartDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 100, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}

public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}
