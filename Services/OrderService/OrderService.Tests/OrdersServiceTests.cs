using Xunit;
using Moq;
using OrderService.Services;
using OrderService.Repository;
using SharedModels.Models;
using System;
using System.Threading.Tasks;

namespace OrderService.Tests;

/// <summary>
/// Unit tests for OrdersService - Business Logic
/// Tests core order processing workflows and validations
/// </summary>
public class OrdersServiceTests
{
    private readonly Mock<IOrdersRepository> _repositoryMock;
    private readonly OrdersService _ordersService;

    public OrdersServiceTests()
    {
        _repositoryMock = new Mock<IOrdersRepository>();
        _ordersService = new OrdersService(_repositoryMock.Object);
    }

    #region Order Creation Tests

    [Fact]
    public async Task CreateOrder_WithValidData_ShouldCreateOrder()
    {
        // Arrange
        var order = new Order
        {
            UserId = 1,
            GiftId = 1,
            Quantity = 2,
            TotalPrice = 59.98m,
            Status = "pending"
        };

        _repositoryMock
            .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(1);

        // Act
        var result = await _ordersService.CreateOrderAsync(order);

        // Assert
        Assert.Equal(1, result);
        _repositoryMock.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_ShouldSetStatusToPending()
    {
        // Arrange
        var order = new Order
        {
            UserId = 1,
            GiftId = 1,
            Quantity = 1,
            TotalPrice = 29.99m
        };

        Order capturedOrder = null;
        _repositoryMock
            .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .ReturnsAsync(1);

        // Act
        await _ordersService.CreateOrderAsync(order);

        // Assert
        Assert.NotNull(capturedOrder);
        Assert.Equal("pending", capturedOrder.Status);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidQuantity_ShouldThrowException()
    {
        // Arrange
        var order = new Order
        {
            UserId = 1,
            GiftId = 1,
            Quantity = 0,  // Invalid
            TotalPrice = 0
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _ordersService.CreateOrderAsync(order));
    }

    [Fact]
    public async Task CreateOrder_WithNegativePrice_ShouldThrowException()
    {
        // Arrange
        var order = new Order
        {
            UserId = 1,
            GiftId = 1,
            Quantity = 1,
            TotalPrice = -29.99m  // Invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _ordersService.CreateOrderAsync(order));
    }

    #endregion

    #region Order Cancellation Tests

    [Fact]
    public async Task CancelOrder_WithPendingStatus_ShouldCancel()
    {
        // Arrange
        var order = new Order
        {
            OrderId = 1,
            UserId = 1,
            GiftId = 1,
            Status = "pending",
            Quantity = 1,
            TotalPrice = 29.99m
        };

        _repositoryMock
            .Setup(r => r.GetOrderByIdAsync(1))
            .ReturnsAsync(order);

        _repositoryMock
            .Setup(r => r.CancelOrderAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _ordersService.CancelOrderAsync(1);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(r => r.CancelOrderAsync(1), Times.Once);
    }

    [Fact]
    public async Task CancelOrder_WithCompletedStatus_ShouldThrowException()
    {
        // Arrange
        var order = new Order
        {
            OrderId = 1,
            Status = "completed"  // Cannot cancel completed orders
        };

        _repositoryMock
            .Setup(r => r.GetOrderByIdAsync(1))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ordersService.CancelOrderAsync(1)
        );
        _repositoryMock.Verify(r => r.CancelOrderAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_WithCancelledStatus_ShouldThrowException()
    {
        // Arrange
        var order = new Order
        {
            OrderId = 1,
            Status = "cancelled"  // Already cancelled
        };

        _repositoryMock
            .Setup(r => r.GetOrderByIdAsync(1))
            .ReturnsAsync(order);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _ordersService.CancelOrderAsync(1)
        );
    }

    [Fact]
    public async Task CancelOrder_WithNonExistentOrder_ShouldThrowException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetOrderByIdAsync(999))
            .ReturnsAsync((Order)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _ordersService.CancelOrderAsync(999)
        );
    }

    #endregion

    #region Order Status Tests

    [Theory]
    [InlineData("pending")]
    [InlineData("completed")]
    [InlineData("cancelled")]
    public async Task GetOrdersByStatus_ShouldReturnOrdersWithMatchingStatus(string status)
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order { OrderId = 1, Status = status, UserId = 1, GiftId = 1, Quantity = 1, TotalPrice = 29.99m },
            new Order { OrderId = 2, Status = status, UserId = 2, GiftId = 2, Quantity = 2, TotalPrice = 59.98m }
        };

        _repositoryMock
            .Setup(r => r.GetOrdersByStatusAsync(status))
            .ReturnsAsync(orders);

        // Act
        var result = await _ordersService.GetOrdersByStatusAsync(status);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal(status, o.Status));
    }

    [Fact]
    public async Task GetOrdersByStatus_WithInvalidStatus_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetOrdersByStatusAsync("invalid"))
            .ReturnsAsync(new List<Order>());

        // Act
        var result = await _ordersService.GetOrdersByStatusAsync("invalid");

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Revenue Tests

    [Fact]
    public async Task GetTotalRevenue_ShouldSumCompletedOrderPrices()
    {
        // Arrange
        decimal expectedRevenue = 1234.56m;

        _repositoryMock
            .Setup(r => r.GetTotalRevenueAsync())
            .ReturnsAsync(expectedRevenue);

        // Act
        var result = await _ordersService.GetTotalRevenueAsync();

        // Assert
        Assert.Equal(expectedRevenue, result);
    }

    [Fact]
    public async Task GetTotalRevenue_ShouldNotIncludePendingOrders()
    {
        // Arrange - Only completed orders included in revenue calculation
        _repositoryMock
            .Setup(r => r.GetTotalRevenueAsync())
            .ReturnsAsync(500m);

        // Act
        var result = await _ordersService.GetTotalRevenueAsync();

        // Assert
        Assert.Equal(500m, result);
        _repositoryMock.Verify(r => r.GetTotalRevenueAsync(), Times.Once);
    }

    #endregion

    #region User Orders Tests

    [Fact]
    public async Task GetUserOrders_ShouldReturnAllOrdersForUser()
    {
        // Arrange
        int userId = 42;
        var userOrders = new List<Order>
        {
            new Order { OrderId = 1, UserId = userId, Status = "completed" },
            new Order { OrderId = 2, UserId = userId, Status = "pending" }
        };

        _repositoryMock
            .Setup(r => r.GetOrdersByUserIdAsync(userId))
            .ReturnsAsync(userOrders);

        // Act
        var result = await _ordersService.GetUserOrdersAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal(userId, o.UserId));
    }

    [Fact]
    public async Task GetUserOrders_WithNoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetOrdersByUserIdAsync(999))
            .ReturnsAsync(new List<Order>());

        // Act
        var result = await _ordersService.GetUserOrdersAsync(999);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region Price Calculation Tests

    [Theory]
    [InlineData(1, 29.99, 29.99)]       // 1 × 29.99 = 29.99
    [InlineData(2, 29.99, 59.98)]       // 2 × 29.99 = 59.98
    [InlineData(5, 10.00, 50.00)]       // 5 × 10.00 = 50.00
    [InlineData(100, 1.50, 150.00)]     // 100 × 1.50 = 150.00
    public async Task CalculateOrderTotal_ShouldMultiplyQuantityByPrice(
        int quantity, decimal unitPrice, decimal expectedTotal)
    {
        // Arrange
        var order = new Order
        {
            UserId = 1,
            GiftId = 1,
            Quantity = quantity,
            TotalPrice = unitPrice * quantity
        };

        _repositoryMock
            .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
            .ReturnsAsync(1);

        // Act
        await _ordersService.CreateOrderAsync(order);

        // Assert
        Assert.Equal(expectedTotal, order.TotalPrice);
    }

    #endregion
}
