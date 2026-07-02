using MassTransit;
using SharedModels.Events;

namespace NotificationService.Consumers;

/// <summary>
/// Consumer for OrderConfirmed event
/// Published by: OrderService after confirming stock reservation
/// Action: Send order confirmation email to customer
/// </summary>
public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
{
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(ILogger<OrderConfirmedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var message = context.Message;
        _logger.LogInformation($"[OrderConfirmedConsumer] Received OrderConfirmed event for Order: {message.OrderId}, User: {message.UserId}");

        try
        {
            // Simulate sending confirmation email
            await SendConfirmationEmailAsync(message.OrderId, message.UserId);
            _logger.LogInformation($"[OrderConfirmedConsumer] Confirmation email sent for Order: {message.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[OrderConfirmedConsumer] Error sending confirmation email for Order: {message.OrderId}");
            // Don't throw - email failures shouldn't fail the saga
        }
    }

    private async Task SendConfirmationEmailAsync(Guid orderId, Guid userId)
    {
        // In a real implementation, this would integrate with SendGrid, Mailgun, or SMTP
        // For now, just simulate the operation
        await Task.Delay(100);
        _logger.LogInformation($"[EmailSimulation] Confirmation email sent to User: {userId} for Order: {orderId}");
    }
}
