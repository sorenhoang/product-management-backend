using System.Text.Json;
using System.Text.Json.Serialization;
using ProductManagement.Application.Common.Interfaces;
using StackExchange.Redis;

namespace ProductManagement.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer multiplexer) : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var db = multiplexer.GetDatabase();
        var value = await db.StringGetAsync(key);
        if (!value.HasValue)
            return default;
        return JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        var db = multiplexer.GetDatabase();
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await db.StringSetAsync(key, json, ttl ?? DefaultTtl);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        var db = multiplexer.GetDatabase();
        await db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            var server = multiplexer.GetServer(multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();
            if (keys.Length > 0)
            {
                var db = multiplexer.GetDatabase();
                await db.KeyDeleteAsync(keys);
            }
        }
        catch
        {
            // No matching keys or Redis unavailable — do nothing
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        try
        {
            var cached = await GetAsync<T>(key, ct);
            if (cached is not null)
                return cached!;

            var result = await factory(ct);
            await SetAsync(key, result, ttl, ct);
            return result;
        }
        catch
        {
            // Redis is unavailable — fall through to factory (best-effort cache)
            return await factory(ct);
        }
    }
}
