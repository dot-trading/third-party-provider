using Microsoft.Extensions.Caching.Memory;
using TradingProject.ThirdParty.Application.Abstractions;

namespace TradingProject.ThirdParty.Infrastructure.Services;

/// <summary>
/// In-process memory-based implementation of <see cref="ICacheService"/>.
/// Used when <c>CacheSettings.Provider</c> is set to <c>"Memory"</c>.
/// Does not require any external infrastructure (no Redis needed).
/// </summary>
public class MemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.TryGetValue(key, out string? value);
        return Task.FromResult(value);
    }

    public Task SetAsync(string key, string value, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        memoryCache.Set(key, value, duration);
        return Task.CompletedTask;
    }
}
