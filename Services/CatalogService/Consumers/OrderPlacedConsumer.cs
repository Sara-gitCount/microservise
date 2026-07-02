using MassTransit;
using SharedModels.Events;
using CatalogService.Interfaces;
using CatalogService.Data;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Consumers;

/// <summary>
/// Consumer for OrderPlaced event
/// Published by: OrderService when a new order is created
/// Action: Check inventory availability and publish InventoryReserved (success) or InventoryFailed (out of stock)
/// </summary>
public class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly CatalogDbContext _dbContext;
    private readonly ILogger<OrderPlacedConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderPlacedConsumer(CatalogDbContext dbContext, ILogger<OrderPlacedConsumer> logger, IPublishEndpoint publishEndpoint)
    {
        _dbContext = dbContext;
        _logger = logger;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var message = context.Message;
        _logger.LogInformation($"[OrderPlacedConsumer] Received OrderPlaced event for Order: {message.OrderId}");

        try
        {
            var reservedItems = new List<ReservedItemDto>();

            // Process each item in the order
            foreach (var item in message.Items)
            {
                // Convert ProductId (Guid) to GiftId (int) 
                var giftIdString = item.ProductId.ToString("N");
                if (!int.TryParse(giftIdString.Substring(0, 8), out var giftId))
                {
                    giftId = item.ProductId.GetHashCode();
                }

                // Check if gift exists and has sufficient quantity
                var gift = await _dbContext.Gifts.FirstOrDefaultAsync(g => g.GiftId == giftId);

                if (gift == null)
                {
                    _logger.LogError($"[OrderPlacedConsumer] Gift not found: {giftId}");
                    
                    // Publish InventoryFailed event (COMPENSATION)
                    var failedEvent = new InventoryFailed
                    {
                        OrderId = message.OrderId,
                        FailedAt = DateTime.UtcNow,
                        Reason = $"Gift not found: {giftId}"
                    };
                    
                    await _publishEndpoint.Publish(failedEvent);
                    _logger.LogWarning($"[OrderPlacedConsumer] InventoryFailed event published for Order: {message.OrderId} - Gift not found");
                    return;
                }

                // Check quantity availability
                if (gift.Quantity < item.Quantity)
                {
                    _logger.LogWarning($"[OrderPlacedConsumer] Insufficient stock for GiftId: {giftId}. Available: {gift.Quantity}, Requested: {item.Quantity}");
                    
                    // Publish InventoryFailed event (COMPENSATION)
                    var failedEvent = new InventoryFailed
                    {
                        OrderId = message.OrderId,
                        FailedAt = DateTime.UtcNow,
                        Reason = $"Insufficient stock. Available: {gift.Quantity}, Requested: {item.Quantity}"
                    };
                    
                    await _publishEndpoint.Publish(failedEvent);
                    _logger.LogWarning($"[OrderPlacedConsumer] InventoryFailed event published for Order: {message.OrderId} - Out of stock");
                    return;
                }

                // HAPPY PATH: Reserve the stock
                gift.Quantity -= item.Quantity;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"[OrderPlacedConsumer] Stock reserved for GiftId: {giftId}. Remaining: {gift.Quantity}");

                // Add to reserved items
                reservedItems.Add(new ReservedItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                });
            }

            // Publish InventoryReserved event (HAPPY PATH)
            var reservedEvent = new InventoryReserved
            {
                OrderId = message.OrderId,
                ReservedAt = DateTime.UtcNow,
                ReservedItems = reservedItems
            };

            await _publishEndpoint.Publish(reservedEvent);
            _logger.LogInformation($"[OrderPlacedConsumer] InventoryReserved event published for Order: {message.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[OrderPlacedConsumer] Error processing OrderPlaced event for Order: {message.OrderId}");
            
            // Publish InventoryFailed event on unexpected errors
            try
            {
                var failedEvent = new InventoryFailed
                {
                    OrderId = message.OrderId,
                    FailedAt = DateTime.UtcNow,
                    Reason = $"Unexpected error: {ex.Message}"
                };
                
                await _publishEndpoint.Publish(failedEvent);
            }
            catch (Exception publishEx)
            {
                _logger.LogError(publishEx, $"[OrderPlacedConsumer] Failed to publish InventoryFailed event");
            }
        }
    }
}
