using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; init; }

    // Computed from the order items after construction (OrderService), so not init-only.
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // Mutable through the order lifecycle.
    public OrderStatus Status { get; set; }

    [Required]
    [MaxLength(36)]
    public string ConfirmationToken { get; init; } = null!;

    public int? UserId { get; init; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [MaxLength(450)]
    public string? SessionId { get; init; }

    [Required]
    [MaxLength(256)]
    public string Email { get; init; } = null!;

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    public Address ShippingAddress { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}

// Order line entity.
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; init; }

    // Price captured at the time the order was placed.
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; init; }
}

[Owned]
public class Address
{
    public string Street { get; init; } = null!;
    public string PostalCode { get; init; } = null!;
    public string City { get; init; } = null!;
}

// Order status.
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Completed,
    Cancelled,
}
