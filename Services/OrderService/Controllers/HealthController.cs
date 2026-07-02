using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using System.Reflection;

namespace OrderService.Controllers;

/// <summary>
/// Health check endpoint for OrderService
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(OrderDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns the health status of OrderService
    /// </summary>
    /// <response code="200">Service is healthy</response>
    /// <response code="503">Service is unhealthy</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Check database connectivity
            var dbHealthy = await _dbContext.Database.CanConnectAsync();

            if (!dbHealthy)
            {
                _logger.LogError("OrderService: Database connection failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    service = "OrderService",
                    database = "Disconnected",
                    version = GetAssemblyVersion(),
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("OrderService: Health check passed");
            return Ok(new
            {
                status = "Healthy",
                service = "OrderService",
                database = "Connected",
                version = GetAssemblyVersion(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OrderService: Health check exception");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "OrderService",
                error = ex.Message,
                version = GetAssemblyVersion(),
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Returns the live status (always 200)
    /// </summary>
    /// <response code="200">Service is running</response>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "Live",
            service = "OrderService",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Returns the ready status (checks all dependencies)
    /// </summary>
    /// <response code="200">Service is ready</response>
    /// <response code="503">Service is not ready</response>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready()
    {
        try
        {
            var dbReady = await _dbContext.Database.CanConnectAsync();

            if (!dbReady)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "NotReady",
                    service = "OrderService",
                    database = "Unavailable"
                });
            }

            return Ok(new
            {
                status = "Ready",
                service = "OrderService",
                database = "Available"
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "NotReady",
                service = "OrderService"
            });
        }
    }

    private string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }
}
