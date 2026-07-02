namespace SharedModels.Events;

/// <summary>
/// Event published by OrderService when cancelling an order (COMPENSATION - due to failed inventory).
/// InventoryService and NotificationService subscribe to this event.
/// </summary>
public class OrderCancelled
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
}
