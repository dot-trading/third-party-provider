using TradingProject.ThirdParty.Application.Abstractions;

namespace TradingProject.ThirdParty.Infrastructure.Services;

/// <summary>
/// No-op implementation of <see cref="ICacheService"/> used when caching is disabled
/// (<c>CacheSettings.Enabled = false</c>).
/// <see cref="GetAsync"/> always returns <c>null</c>, and <see cref="SetAsync"/> does nothing.
/// This avoids null-checks in callers while bypassing Redis entirely.
/// </summary>
public class NullCacheService : ICacheService
{
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);

    public Task SetAsync(string key, string value, TimeSpan duration, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
