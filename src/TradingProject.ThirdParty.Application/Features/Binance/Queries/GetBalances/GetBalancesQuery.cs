using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;

public record GetBalancesQuery : IRequest<Dictionary<string, double>>;

public class GetBalancesQueryHandler(
    IBinanceService binanceService,
    IConnectionMultiplexer redis,
    ILogger<GetBalancesQueryHandler> logger) : IRequestHandler<GetBalancesQuery, Dictionary<string, double>>
{
    private const string Key = "Binance:GetBalances";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public async Task<Dictionary<string, double>> Handle(GetBalancesQuery request, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();

        // Try to get from cache
        var cachedData = await db.StringGetAsync(Key);

        if (cachedData.HasValue)
        {
            logger.LogInformation("Returning cached balances for key {Key}", Key);
            return JsonSerializer.Deserialize<Dictionary<string, double>>(cachedData.ToString())
                   ?? new Dictionary<string, double>();
        }

        logger.LogInformation("No cached balances found for key {Key}, fetching from Binance", Key);

        // Fetch from service
        var balances = await binanceService.GetBalancesAsync(cancellationToken);

        // Store in cache
        var serialized = JsonSerializer.Serialize(balances);
        await db.StringSetAsync(Key, serialized, CacheDuration);

        logger.LogInformation("Stored balances in cache for key {Key}", Key);

        return balances;
    }
}
