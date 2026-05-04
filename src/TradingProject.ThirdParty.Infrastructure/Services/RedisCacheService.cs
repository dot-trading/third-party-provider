using StackExchange.Redis;
using TradingProject.ThirdParty.Application.Abstractions;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        var value = await db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetAsync(string key, string value, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(key, value, duration);
    }
}
