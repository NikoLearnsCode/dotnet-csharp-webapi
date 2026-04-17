namespace dotnet_backend_2.Data.Entities;

public class Cart
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string? SessionId { get; set; }
    public List<CartItem> Items { get; set; } = [];
}
