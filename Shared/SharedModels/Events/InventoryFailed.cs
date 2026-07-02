namespace SharedModels.Events;

/// <summary>
/// Event published by InventoryService when stock reservation fails (out of stock).
/// OrderService subscribes to cancel the order (COMPENSATION).
/// </summary>
public class InventoryFailed
{
    public Guid OrderId { get; set; }
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
}
