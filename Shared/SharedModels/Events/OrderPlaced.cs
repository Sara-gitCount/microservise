namespace SharedModels.Events;

/// <summary>
/// Event published by OrderService when a new order is created.
/// InventoryService subscribes to this event to attempt stock reservation.
/// </summary>
public class OrderPlaced
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
}

public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
