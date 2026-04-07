namespace ProductManagement.Application.Common.Interfaces;

public interface ICacheService
{
    /// <summary>Get a cached value by key. Returns null if not found.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>Set a value with an optional TTL. If ttl is null, uses the default TTL.</summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken ct = default);

    /// <summary>Remove a single key.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Remove all keys matching a prefix pattern (e.g. "products:list:*").</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>
    /// Get-or-set: if the key exists return it, otherwise call factory,
    /// cache the result, and return it. Redis failure falls through to factory.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default);
}
