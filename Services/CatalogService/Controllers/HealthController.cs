using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using StackExchange.Redis;
using System.Reflection;

namespace CatalogService.Controllers;

/// <summary>
/// Health check endpoint for CatalogService
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly CatalogDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<HealthController> _logger;

    public HealthController(CatalogDbContext dbContext, IConnectionMultiplexer redis, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Returns the health status of CatalogService including database and cache
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
                _logger.LogError("CatalogService: Database connection failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    service = "CatalogService",
                    database = "Disconnected",
                    cache = "Unknown",
                    version = GetAssemblyVersion(),
                    timestamp = DateTime.UtcNow
                });
            }

            // Check Redis connectivity
            var cacheHealthy = CheckRedisHealth();

            if (!cacheHealthy)
            {
                _logger.LogWarning("CatalogService: Redis connection degraded (service still running)");
                return Ok(new
                {
                    status = "Degraded",
                    service = "CatalogService",
                    database = "Connected",
                    cache = "Disconnected",
                    version = GetAssemblyVersion(),
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("CatalogService: Health check passed");
            return Ok(new
            {
                status = "Healthy",
                service = "CatalogService",
                database = "Connected",
                cache = "Connected",
                version = GetAssemblyVersion(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CatalogService: Health check exception");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "CatalogService",
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
            service = "CatalogService",
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
            var cacheReady = CheckRedisHealth();

            if (!dbReady || !cacheReady)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "NotReady",
                    service = "CatalogService",
                    database = dbReady ? "Available" : "Unavailable",
                    cache = cacheReady ? "Available" : "Unavailable"
                });
            }

            return Ok(new
            {
                status = "Ready",
                service = "CatalogService",
                database = "Available",
                cache = "Available"
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "NotReady",
                service = "CatalogService"
            });
        }
    }

    private bool CheckRedisHealth()
    {
        try
        {
            return _redis.IsConnected;
        }
        catch
        {
            return false;
        }
    }

    private string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }
}
