namespace SharedModels.Events;

/// <summary>
/// Event published by InventoryService when stock is successfully reserved.
/// OrderService subscribes to confirm the order.
/// </summary>
public class InventoryReserved
{
    public Guid OrderId { get; set; }
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public List<ReservedItemDto> ReservedItems { get; set; } = new();
}

public class ReservedItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
