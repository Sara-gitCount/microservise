using Xunit;
using Moq;
using LotteryService.Services;
using LotteryService.Repository;
using SharedModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LotteryService.Tests;

/// <summary>
/// Unit tests for LotteryDrawService - Critical Business Logic
/// Tests lottery draw algorithm and fairness guarantees
/// </summary>
public class LotteryDrawServiceTests
{
    private readonly Mock<ILotteryRepository> _repositoryMock;
    private readonly LotteryDrawService _lotteryService;

    public LotteryDrawServiceTests()
    {
        _repositoryMock = new Mock<ILotteryRepository>();
        _lotteryService = new LotteryDrawService(_repositoryMock.Object);
    }

    #region Draw Creation Tests

    [Fact]
    public async Task CreateDraw_WithValidData_ShouldCreateDraw()
    {
        // Arrange
        var draw = new LotteryDraw
        {
            Name = "Weekly Draw",
            DrawDate = DateTime.UtcNow.AddDays(7),
            Prize = 1000m,
            Status = "scheduled"
        };

        _repositoryMock
            .Setup(r => r.CreateDrawAsync(It.IsAny<LotteryDraw>()))
            .ReturnsAsync(1);

        // Act
        var result = await _lotteryService.CreateDrawAsync(draw);

        // Assert
        Assert.Equal(1, result);
        _repositoryMock.Verify(r => r.CreateDrawAsync(It.IsAny<LotteryDraw>()), Times.Once);
    }

    [Fact]
    public async Task CreateDraw_ShouldSetStatusToScheduled()
    {
        // Arrange
        var draw = new LotteryDraw
        {
            Name = "Monthly Draw",
            DrawDate = DateTime.UtcNow.AddDays(30),
            Prize = 5000m
        };

        LotteryDraw capturedDraw = null;
        _repositoryMock
            .Setup(r => r.CreateDrawAsync(It.IsAny<LotteryDraw>()))
            .Callback<LotteryDraw>(d => capturedDraw = d)
            .ReturnsAsync(1);

        // Act
        await _lotteryService.CreateDrawAsync(draw);

        // Assert
        Assert.NotNull(capturedDraw);
        Assert.Equal("scheduled", capturedDraw.Status);
    }

    [Fact]
    public async Task CreateDraw_WithPastDate_ShouldThrowException()
    {
        // Arrange
        var draw = new LotteryDraw
        {
            Name = "Invalid Draw",
            DrawDate = DateTime.UtcNow.AddDays(-1),  // Past date
            Prize = 1000m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _lotteryService.CreateDrawAsync(draw)
        );
    }

    [Fact]
    public async Task CreateDraw_WithNegativePrize_ShouldThrowException()
    {
        // Arrange
        var draw = new LotteryDraw
        {
            Name = "Invalid Draw",
            DrawDate = DateTime.UtcNow.AddDays(7),
            Prize = -100m  // Invalid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _lotteryService.CreateDrawAsync(draw)
        );
    }

    [Fact]
    public async Task CreateDraw_WithZeroPrize_ShouldThrowException()
    {
        // Arrange
        var draw = new LotteryDraw
        {
            Name = "Invalid Draw",
            DrawDate = DateTime.UtcNow.AddDays(7),
            Prize = 0m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _lotteryService.CreateDrawAsync(draw)
        );
    }

    #endregion

    #region Lottery Entry Tests

    [Fact]
    public async Task EnterLottery_WithValidUserAndDraw_ShouldCreateTicket()
    {
        // Arrange
        int userId = 1;
        int drawId = 1;

        var draw = new LotteryDraw
        {
            DrawId = drawId,
            Name = "Test Draw",
            Status = "scheduled"
        };

        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(drawId))
            .ReturnsAsync(draw);

        _repositoryMock
            .Setup(r => r.CreateTicketAsync(It.IsAny<LotteryTicket>()))
            .ReturnsAsync(1);

        // Act
        var result = await _lotteryService.EnterLotteryAsync(userId, drawId);

        // Assert
        Assert.Equal(1, result);
        _repositoryMock.Verify(r => r.CreateTicketAsync(It.IsAny<LotteryTicket>()), Times.Once);
    }

    [Fact]
    public async Task EnterLottery_WithCompletedDraw_ShouldThrowException()
    {
        // Arrange
        int drawId = 1;
        var draw = new LotteryDraw
        {
            DrawId = drawId,
            Status = "completed"  // Cannot enter completed draw
        };

        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(drawId))
            .ReturnsAsync(draw);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _lotteryService.EnterLotteryAsync(1, drawId)
        );
    }

    [Fact]
    public async Task EnterLottery_WithNonExistentDraw_ShouldThrowException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(999))
            .ReturnsAsync((LotteryDraw)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _lotteryService.EnterLotteryAsync(1, 999)
        );
    }

    [Fact]
    public async Task EnterLottery_SameUserTwice_ShouldCreateTwoTickets()
    {
        // Arrange
        int userId = 1;
        int drawId = 1;

        var draw = new LotteryDraw
        {
            DrawId = drawId,
            Status = "scheduled"
        };

        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(drawId))
            .ReturnsAsync(draw);

        _repositoryMock
            .Setup(r => r.CreateTicketAsync(It.IsAny<LotteryTicket>()))
            .ReturnsAsync(1);  // Returns different IDs for each call

        // Act
        var ticket1 = await _lotteryService.EnterLotteryAsync(userId, drawId);
        var ticket2 = await _lotteryService.EnterLotteryAsync(userId, drawId);

        // Assert
        _repositoryMock.Verify(r => r.CreateTicketAsync(It.IsAny<LotteryTicket>()), Times.Exactly(2));
    }

    #endregion

    #region Lottery Draw Algorithm Tests

    [Fact]
    public async Task ConductDraw_WithNoTickets_ShouldThrowException()
    {
        // Arrange
        int drawId = 1;
        var draw = new LotteryDraw { DrawId = drawId, Status = "scheduled" };

        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(drawId))
            .ReturnsAsync(draw);

        _repositoryMock
            .Setup(r => r.GetTicketsForDrawAsync(drawId))
            .ReturnsAsync(new List<LotteryTicket>());  // No tickets

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _lotteryService.ConductDrawAsync(drawId)
        );
    }

    [Fact]
    public async Task ConductDraw_ShouldSelectOneWinner()
    {
        // Arrange
        int drawId = 1;
        var draw = new LotteryDraw { DrawId = drawId, Status = "scheduled" };
        var tickets = new List<LotteryTicket>
        {
            new LotteryTicket { TicketId = 1, UserId = 100, DrawId = drawId },
            new LotteryTicket { TicketId = 2, UserId = 101, DrawId = drawId },
            new LotteryTicket { TicketId = 3, UserId = 102, DrawId = drawId }
        };

        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(drawId))
            .ReturnsAsync(draw);

        _repositoryMock
            .Setup(r => r.GetTicketsForDrawAsync(drawId))
            .ReturnsAsync(tickets);

        _repositoryMock
            .Setup(r => r.MarkWinnerAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.UpdateDrawAsync(It.IsAny<LotteryDraw>()))
            .ReturnsAsync(true);

        // Act
        await _lotteryService.ConductDrawAsync(drawId);

        // Assert - Exactly one winner should be marked
        _repositoryMock.Verify(
            r => r.MarkWinnerAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ConductDraw_WinnerShouldBeOneOfTheParticipants()
    {
        // Arrange
        int drawId = 1;
        var draw = new LotteryDraw { DrawId = drawId, Status = "scheduled" };
        var tickets = new List<LotteryTicket>
        {
            new LotteryTicket { TicketId = 1, UserId = 100, DrawId = drawId },
            new LotteryTicket { TicketId = 2, UserId = 101, DrawId = drawId }
        };

        var validUserIds = new HashSet<int> { 100, 101 };

        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(drawId))
            .ReturnsAsync(draw);

        _repositoryMock
            .Setup(r => r.GetTicketsForDrawAsync(drawId))
            .ReturnsAsync(tickets);

        _repositoryMock
            .Setup(r => r.MarkWinnerAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.UpdateDrawAsync(It.IsAny<LotteryDraw>()))
            .ReturnsAsync(true);

        // Act
        await _lotteryService.ConductDrawAsync(drawId);

        // Assert
        _repositoryMock.Verify(
            r => r.MarkWinnerAsync(
                It.Is<int>(winnerUserId => validUserIds.Contains(winnerUserId)),
                drawId
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task ConductDraw_ShouldUpdateDrawStatusToCompleted()
    {
        // Arrange
        int drawId = 1;
        var draw = new LotteryDraw { DrawId = drawId, Status = "scheduled" };
        var tickets = new List<LotteryTicket>
        {
            new LotteryTicket { TicketId = 1, UserId = 100, DrawId = drawId }
        };

        LotteryDraw updatedDraw = null;
        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(drawId))
            .ReturnsAsync(draw);

        _repositoryMock
            .Setup(r => r.GetTicketsForDrawAsync(drawId))
            .ReturnsAsync(tickets);

        _repositoryMock
            .Setup(r => r.MarkWinnerAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.UpdateDrawAsync(It.IsAny<LotteryDraw>()))
            .Callback<LotteryDraw>(d => updatedDraw = d)
            .ReturnsAsync(true);

        // Act
        await _lotteryService.ConductDrawAsync(drawId);

        // Assert
        Assert.NotNull(updatedDraw);
        Assert.Equal("completed", updatedDraw.Status);
    }

    [Fact]
    public async Task ConductDraw_WithCompletedDraw_ShouldThrowException()
    {
        // Arrange
        var draw = new LotteryDraw { DrawId = 1, Status = "completed" };

        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(1))
            .ReturnsAsync(draw);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _lotteryService.ConductDrawAsync(1)
        );
    }

    #endregion

    #region Draw Fairness Tests

    [Fact]
    public async Task ConductDraw_MultipleRuns_ShouldDistributeWinnersUniformly()
    {
        // Arrange - Run multiple draws to check fairness
        var winnerCounts = new Dictionary<int, int>();
        int numberOfRuns = 100;
        int ticketsPerDraw = 10;

        for (int run = 0; run < numberOfRuns; run++)
        {
            int drawId = run + 1;
            var draw = new LotteryDraw { DrawId = drawId, Status = "scheduled" };
            var tickets = Enumerable.Range(1, ticketsPerDraw)
                .Select(i => new LotteryTicket { TicketId = i, UserId = i, DrawId = drawId })
                .ToList();

            _repositoryMock
                .Setup(r => r.GetDrawByIdAsync(drawId))
                .ReturnsAsync(draw);

            _repositoryMock
                .Setup(r => r.GetTicketsForDrawAsync(drawId))
                .ReturnsAsync(tickets);

            int winnerUserId = 0;
            _repositoryMock
                .Setup(r => r.MarkWinnerAsync(It.IsAny<int>(), drawId))
                .Callback<int, int>((uid, _) => winnerUserId = uid)
                .ReturnsAsync(true);

            _repositoryMock
                .Setup(r => r.UpdateDrawAsync(It.IsAny<LotteryDraw>()))
                .ReturnsAsync(true);

            // Act
            await _lotteryService.ConductDrawAsync(drawId);

            // Track winner
            if (!winnerCounts.ContainsKey(winnerUserId))
                winnerCounts[winnerUserId] = 0;
            winnerCounts[winnerUserId]++;
        }

        // Assert - Each user should win approximately equal times (fairness check)
        // With 100 runs and 10 users, each should win ~10 times
        // Allow 50% variance (5-15 wins each)
        var averageWins = numberOfRuns / (double)ticketsPerDraw;
        var tolerance = averageWins * 0.5;  // 50% variance tolerance

        foreach (var count in winnerCounts.Values)
        {
            Assert.InRange(count, averageWins - tolerance, averageWins + tolerance);
        }
    }

    #endregion

    #region Draw Query Tests

    [Fact]
    public async Task GetActiveDraws_ShouldReturnOnlyScheduledDraws()
    {
        // Arrange
        var draws = new List<LotteryDraw>
        {
            new LotteryDraw { DrawId = 1, Status = "scheduled" },
            new LotteryDraw { DrawId = 2, Status = "scheduled" },
            new LotteryDraw { DrawId = 3, Status = "completed" }
        };

        _repositoryMock
            .Setup(r => r.GetDrawsByStatusAsync("scheduled"))
            .ReturnsAsync(draws.Where(d => d.Status == "scheduled").ToList());

        // Act
        var result = await _lotteryService.GetActiveDrawsAsync();

        // Assert
        Assert.All(result, d => Assert.Equal("scheduled", d.Status));
    }

    [Fact]
    public async Task GetDraw_WithValidId_ShouldReturnDraw()
    {
        // Arrange
        var draw = new LotteryDraw { DrawId = 1, Name = "Test Draw" };

        _repositoryMock
            .Setup(r => r.GetDrawByIdAsync(1))
            .ReturnsAsync(draw);

        // Act
        var result = await _lotteryService.GetDrawAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Draw", result.Name);
    }

    #endregion
}
