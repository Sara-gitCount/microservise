using StackExchange.Redis;
using System.Text.Json;

namespace CatalogService.Services.Cache;

/// <summary>
/// Redis cache service implementation
/// Provides cache-aside pattern for data retrieval with TTL support
/// Default TTL: 10 minutes
/// </summary>
public class CacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<CacheService> _logger;
    private readonly IDatabase _database;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(10);

    public CacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<CacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
        _database = _connectionMultiplexer.GetDatabase();
    }

    /// <summary>
    /// Get value from cache by key
    /// Returns null if key doesn't exist or has expired
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNull)
            {
                _logger.LogDebug($"[CacheService] CacheMiss - Key: {key}");
                return default;
            }

            _logger.LogDebug($"[CacheService] CacheHit - Key: {key}");
            var deserialized = JsonSerializer.Deserialize<T>(value.ToString());
            return deserialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CacheService] Error retrieving from cache - Key: {key}");
            return default; // Fail gracefully, return null
        }
    }

    /// <summary>
    /// Set value in cache with expiration
    /// Uses default TTL (10 minutes) if expiration not specified
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var ttl = expiration ?? _defaultExpiration;
            
            await _database.StringSetAsync(key, serialized, ttl);
            _logger.LogDebug($"[CacheService] Set cache - Key: {key}, TTL: {ttl.TotalMinutes} minutes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CacheService] Error setting cache - Key: {key}");
            // Fail gracefully, don't throw - cache is not critical
        }
    }

    /// <summary>
    /// Remove value from cache by key
    /// </summary>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug($"[CacheService] Removed from cache - Key: {key}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CacheService] Error removing from cache - Key: {key}");
            // Fail gracefully
        }
    }

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[CacheService] Error checking cache key existence - Key: {key}");
            return false;
        }
    }
}
