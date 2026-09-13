using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Caching;

public sealed class RedisCacheService(IDistributedCache distributedCache)
    : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await distributedCache.GetAsync(key, cancellationToken);

        if (bytes is null || bytes.Length == 0)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? timeToLive = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions();

        if (timeToLive.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = timeToLive.Value;
        }

        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

        return distributedCache.SetAsync(key, bytes, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return distributedCache.RemoveAsync(key, cancellationToken);
    }
}
