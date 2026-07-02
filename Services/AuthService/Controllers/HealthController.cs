using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Data;
using System.Reflection;

namespace AuthService.Controllers;

/// <summary>
/// Health check endpoint for AuthService
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AuthDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AuthDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns the health status of AuthService
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
                _logger.LogError("AuthService: Database connection failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "Unhealthy",
                    service = "AuthService",
                    database = "Disconnected",
                    version = GetAssemblyVersion(),
                    timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("AuthService: Health check passed");
            return Ok(new
            {
                status = "Healthy",
                service = "AuthService",
                database = "Connected",
                version = GetAssemblyVersion(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AuthService: Health check exception");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "AuthService",
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
            service = "AuthService",
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
                    service = "AuthService",
                    database = "Unavailable"
                });
            }

            return Ok(new
            {
                status = "Ready",
                service = "AuthService",
                database = "Available"
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "NotReady",
                service = "AuthService"
            });
        }
    }

    private string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }
}
