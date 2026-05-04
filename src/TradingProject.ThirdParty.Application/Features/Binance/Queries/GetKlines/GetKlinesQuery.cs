using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetKlines;

public record GetKlinesQuery(string Symbol, string Interval = "1h", int Limit = 24) : IRequest<List<Kline>>;

public class GetKlinesQueryHandler(
    IBinanceService binanceService,
    IConnectionMultiplexer redis,
    ILogger<GetKlinesQueryHandler> logger) : IRequestHandler<GetKlinesQuery, List<Kline>>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<List<Kline>> Handle(GetKlinesQuery request, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var key = $"Binance:Klines:{request.Symbol}:{request.Interval}:{request.Limit}";

        // Try to get from cache
        var cachedData = await db.StringGetAsync(key);

        if (cachedData.HasValue)
        {
            logger.LogInformation("Returning cached klines for key {Key}", key);
            return JsonSerializer.Deserialize<List<Kline>>(cachedData.ToString())
                   ?? new List<Kline>();
        }

        logger.LogInformation("No cached klines found for key {Key}, fetching from Binance", key);

        // Fetch from service
        var klines = await binanceService.GetKlinesAsync(request.Symbol, request.Interval, request.Limit, cancellationToken);

        // Store in cache
        var serialized = JsonSerializer.Serialize(klines);
        await db.StringSetAsync(key, serialized, CacheDuration);

        logger.LogInformation("Stored klines in cache for key {Key}", key);

        return klines;
    }
}
