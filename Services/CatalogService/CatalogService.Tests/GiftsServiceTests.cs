using Xunit;
using Moq;
using CatalogService.Services;
using CatalogService.Repository;
using SharedModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CatalogService.Tests;

/// <summary>
/// Unit tests for GiftsService - Catalog Business Logic
/// Tests gift filtering, search, and inventory management
/// </summary>
public class GiftsServiceTests
{
    private readonly Mock<IGiftsRepository> _repositoryMock;
    private readonly GiftsService _giftsService;

    public GiftsServiceTests()
    {
        _repositoryMock = new Mock<IGiftsRepository>();
        _giftsService = new GiftsService(_repositoryMock.Object);
    }

    #region Gift Filtering Tests

    [Fact]
    public async Task FilterGifts_ByCategory_ShouldReturnMatchingGifts()
    {
        // Arrange
        string category = "Electronics";
        var gifts = new List<Gift>
        {
            new Gift { GiftId = 1, Name = "Laptop", Category = "Electronics", Price = 799.99m },
            new Gift { GiftId = 2, Name = "Phone", Category = "Electronics", Price = 499.99m },
            new Gift { GiftId = 3, Name = "Book", Category = "Books", Price = 19.99m }
        };

        _repositoryMock
            .Setup(r => r.GetGiftsByCategoryAsync(category))
            .ReturnsAsync(gifts.Where(g => g.Category == category).ToList());

        // Act
        var result = await _giftsService.GetGiftsByCategoryAsync(category);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, g => Assert.Equal(category, g.Category));
    }

    [Fact]
    public async Task FilterGifts_ByPriceRange_ShouldReturnGiftsInRange()
    {
        // Arrange
        decimal minPrice = 100m;
        decimal maxPrice = 500m;

        var gifts = new List<Gift>
        {
            new Gift { GiftId = 1, Name = "Phone", Price = 299.99m },
            new Gift { GiftId = 2, Name = "Laptop", Price = 799.99m },
            new Gift { GiftId = 3, Name = "Tablet", Price = 399.99m }
        };

        _repositoryMock
            .Setup(r => r.GetGiftsByPriceRangeAsync(minPrice, maxPrice))
            .ReturnsAsync(gifts.Where(g => g.Price >= minPrice && g.Price <= maxPrice).ToList());

        // Act
        var result = await _giftsService.GetGiftsByPriceRangeAsync(minPrice, maxPrice);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, g => Assert.InRange(g.Price, minPrice, maxPrice));
    }

    [Fact]
    public async Task FilterGifts_ByPriceRange_WithInvalidRange_ShouldThrowException()
    {
        // Arrange - Max price less than min price
        decimal minPrice = 500m;
        decimal maxPrice = 100m;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _giftsService.GetGiftsByPriceRangeAsync(minPrice, maxPrice)
        );
    }

    [Fact]
    public async Task FilterGifts_ByPriceRange_WithNegativePrice_ShouldThrowException()
    {
        // Arrange
        decimal minPrice = -100m;
        decimal maxPrice = 500m;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _giftsService.GetGiftsByPriceRangeAsync(minPrice, maxPrice)
        );
    }

    #endregion

    #region Gift Search Tests

    [Theory]
    [InlineData("laptop", "Laptop")]
    [InlineData("LAPTOP", "Laptop")]
    [InlineData("laptop pro", "Laptop Pro 15 inch")]
    public async Task SearchGifts_ShouldBeCaseInsensitive(string searchTerm, string expectedGiftName)
    {
        // Arrange
        var gifts = new List<Gift>
        {
            new Gift { GiftId = 1, Name = "Laptop", Description = "High performance" },
            new Gift { GiftId = 2, Name = "Laptop Pro 15 inch", Description = "Professional grade" },
            new Gift { GiftId = 3, Name = "Phone", Description = "Mobile device" }
        };

        _repositoryMock
            .Setup(r => r.SearchGiftsAsync(It.IsAny<string>()))
            .ReturnsAsync(gifts.Where(g =>
                g.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                g.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList());

        // Act
        var result = await _giftsService.SearchGiftsAsync(searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, g => g.Name.Contains(expectedGiftName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchGifts_ShouldSearchBothNameAndDescription()
    {
        // Arrange
        string searchTerm = "gaming";
        var gifts = new List<Gift>
        {
            new Gift { GiftId = 1, Name = "Gaming Laptop", Description = "For work" },
            new Gift { GiftId = 2, Name = "Laptop", Description = "For gaming purposes" },
            new Gift { GiftId = 3, Name = "Phone", Description = "Mobile device" }
        };

        _repositoryMock
            .Setup(r => r.SearchGiftsAsync(searchTerm))
            .ReturnsAsync(gifts.Where(g =>
                g.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                g.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            ).ToList());

        // Act
        var result = await _giftsService.SearchGiftsAsync(searchTerm);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);  // Both matching gifts
    }

    [Fact]
    public async Task SearchGifts_WithNoResults_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.SearchGiftsAsync("nonexistent"))
            .ReturnsAsync(new List<Gift>());

        // Act
        var result = await _giftsService.SearchGiftsAsync("nonexistent");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchGifts_WithEmptyString_ShouldReturnAllGifts()
    {
        // Arrange
        var allGifts = new List<Gift>
        {
            new Gift { GiftId = 1, Name = "Laptop" },
            new Gift { GiftId = 2, Name = "Phone" },
            new Gift { GiftId = 3, Name = "Tablet" }
        };

        _repositoryMock
            .Setup(r => r.SearchGiftsAsync(""))
            .ReturnsAsync(allGifts);

        // Act
        var result = await _giftsService.SearchGiftsAsync("");

        // Assert
        Assert.Equal(3, result.Count);
    }

    #endregion

    #region Inventory Management Tests

    [Fact]
    public async Task UpdateInventory_WithPositiveQuantity_ShouldIncreaseStock()
    {
        // Arrange
        int giftId = 1;
        int quantityToAdd = 10;
        var gift = new Gift { GiftId = giftId, Name = "Laptop", Stock = 5 };

        _repositoryMock
            .Setup(r => r.GetGiftByIdAsync(giftId))
            .ReturnsAsync(gift);

        _repositoryMock
            .Setup(r => r.UpdateGiftAsync(It.IsAny<Gift>()))
            .ReturnsAsync(true);

        // Act
        var result = await _giftsService.UpdateInventoryAsync(giftId, quantityToAdd);

        // Assert
        Assert.True(result);
        Assert.Equal(15, gift.Stock);  // 5 + 10
    }

    [Fact]
    public async Task UpdateInventory_WithNegativeQuantity_ShouldDecreaseStock()
    {
        // Arrange
        int giftId = 1;
        int quantityToRemove = -3;
        var gift = new Gift { GiftId = giftId, Name = "Phone", Stock = 10 };

        _repositoryMock
            .Setup(r => r.GetGiftByIdAsync(giftId))
            .ReturnsAsync(gift);

        _repositoryMock
            .Setup(r => r.UpdateGiftAsync(It.IsAny<Gift>()))
            .ReturnsAsync(true);

        // Act
        var result = await _giftsService.UpdateInventoryAsync(giftId, quantityToRemove);

        // Assert
        Assert.True(result);
        Assert.Equal(7, gift.Stock);  // 10 - 3
    }

    [Fact]
    public async Task UpdateInventory_ToNegativeStock_ShouldThrowException()
    {
        // Arrange
        int giftId = 1;
        int quantityToRemove = -15;  // Would result in negative stock
        var gift = new Gift { GiftId = giftId, Stock = 10 };

        _repositoryMock
            .Setup(r => r.GetGiftByIdAsync(giftId))
            .ReturnsAsync(gift);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _giftsService.UpdateInventoryAsync(giftId, quantityToRemove)
        );
    }

    [Fact]
    public async Task UpdateInventory_WithNonExistentGift_ShouldThrowException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetGiftByIdAsync(999))
            .ReturnsAsync((Gift)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _giftsService.UpdateInventoryAsync(999, 5)
        );
    }

    #endregion

    #region Stock Availability Tests

    [Theory]
    [InlineData(1, true)]   // In stock
    [InlineData(0, false)]  // Out of stock
    [InlineData(100, true)] // Well stocked
    public async Task IsGiftInStock_ShouldCheckStockGreaterThanZero(int stock, bool expectedInStock)
    {
        // Arrange
        var gift = new Gift { GiftId = 1, Name = "Test Gift", Stock = stock };

        _repositoryMock
            .Setup(r => r.GetGiftByIdAsync(1))
            .ReturnsAsync(gift);

        // Act
        var result = await _giftsService.IsGiftInStockAsync(1);

        // Assert
        Assert.Equal(expectedInStock, result);
    }

    [Fact]
    public async Task GetStockQuantity_ShouldReturnCurrentStock()
    {
        // Arrange
        var gift = new Gift { GiftId = 1, Name = "Laptop", Stock = 42 };

        _repositoryMock
            .Setup(r => r.GetGiftByIdAsync(1))
            .ReturnsAsync(gift);

        // Act
        var result = await _giftsService.GetStockQuantityAsync(1);

        // Assert
        Assert.Equal(42, result);
    }

    #endregion

    #region Gift Retrieval Tests

    [Fact]
    public async Task GetAllGifts_ShouldReturnAllGifts()
    {
        // Arrange
        var gifts = new List<Gift>
        {
            new Gift { GiftId = 1, Name = "Laptop" },
            new Gift { GiftId = 2, Name = "Phone" },
            new Gift { GiftId = 3, Name = "Tablet" }
        };

        _repositoryMock
            .Setup(r => r.GetAllGiftsAsync())
            .ReturnsAsync(gifts);

        // Act
        var result = await _giftsService.GetAllGiftsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetGiftById_WithValidId_ShouldReturnGift()
    {
        // Arrange
        var gift = new Gift { GiftId = 1, Name = "Laptop", Price = 799.99m };

        _repositoryMock
            .Setup(r => r.GetGiftByIdAsync(1))
            .ReturnsAsync(gift);

        // Act
        var result = await _giftsService.GetGiftByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal(799.99m, result.Price);
    }

    [Fact]
    public async Task GetGiftById_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetGiftByIdAsync(999))
            .ReturnsAsync((Gift)null);

        // Act
        var result = await _giftsService.GetGiftByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Price Validation Tests

    [Theory]
    [InlineData(0.99)]
    [InlineData(1.00)]
    [InlineData(9999.99)]
    public async Task CreateGift_WithValidPrice_ShouldSucceed(decimal price)
    {
        // Arrange
        var gift = new Gift
        {
            Name = "Test Gift",
            Price = price,
            Stock = 10
        };

        _repositoryMock
            .Setup(r => r.CreateGiftAsync(It.IsAny<Gift>()))
            .ReturnsAsync(1);

        // Act
        var result = await _giftsService.CreateGiftAsync(gift);

        // Assert
        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData(-1.00)]     // Negative price
    [InlineData(0.00)]      // Zero price
    public async Task CreateGift_WithInvalidPrice_ShouldThrowException(decimal invalidPrice)
    {
        // Arrange
        var gift = new Gift
        {
            Name = "Test Gift",
            Price = invalidPrice,
            Stock = 10
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _giftsService.CreateGiftAsync(gift)
        );
    }

    #endregion
}
