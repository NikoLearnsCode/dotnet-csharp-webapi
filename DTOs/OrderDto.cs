using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using dotnet_backend_2.Data.Entities;

namespace dotnet_backend_2.DTOs;

// Request DTO
public class CreateOrderFromCartDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = null!;

    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    public AddressDto ShippingAddress { get; set; } = null!;
}

public class AddressDto
{
    [Required]
    [StringLength(200)]
    public string Street { get; set; } = null!;

    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string City { get; set; } = null!;
}

// Response DTOs
public class OrderResponseDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string ConfirmationToken { get; set; } = string.Empty;

    [JsonIgnore]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmountFormatted => Math.Round(TotalAmount, 2);

    public string Status { get; set; } = string.Empty;

    public int? UserId { get; set; }
    public string? SessionId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    public AddressDto ShippingAddress { get; set; } = null!;

    public List<OrderItemResponseDto> OrderItems { get; set; } = [];
}

public class OrderItemResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    // Flattened fields from Product
    public string ProductName { get; set; } = string.Empty;
    public string ProductUrlSlug { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }

    public int Quantity { get; set; }

    [JsonIgnore]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPriceFormatted => Math.Round(UnitPrice, 2);

    [JsonIgnore]
    public decimal LineTotal { get; set; }

    [JsonPropertyName("lineTotal")]
    public decimal LineTotalFormatted => Math.Round(LineTotal, 2);
}
