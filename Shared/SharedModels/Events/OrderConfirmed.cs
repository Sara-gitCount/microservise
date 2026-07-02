namespace SharedModels.Events;

/// <summary>
/// Event published by OrderService after confirming stock reservation.
/// NotificationService subscribes to send confirmation email to customer.
/// </summary>
public class OrderConfirmed
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
}
