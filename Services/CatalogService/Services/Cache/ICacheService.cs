namespace CatalogService.Services.Cache;

/// <summary>
/// Interface for cache service using Redis
/// Implements cache-aside pattern with configurable TTL
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}
