using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace dotnet_backend_2.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }

    [Required]
    [MaxLength(36)]
    public string ConfirmationToken { get; set; } = null!;

    public int? UserId { get; set; }
    public User? User { get; set; }

    [MaxLength(450)]
    public string? SessionId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = null!;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

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
    public int Quantity { get; set; }

    // Price captured at the time the order was placed.
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }
}

[Owned]
public class Address
{
    public string Street { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
    public string City { get; set; } = null!;
}

// Order status.
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Completed,
    Cancelled
}

