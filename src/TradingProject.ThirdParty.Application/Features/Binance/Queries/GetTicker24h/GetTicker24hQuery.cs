using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTicker24h;

public record GetTicker24HQuery(string Symbol) : IRequest<Ticker24h?>;

public class GetTicker24HQueryHandler(
    IBinanceService binanceService,
    IConnectionMultiplexer redis,
    ILogger<GetTicker24HQueryHandler> logger) : IRequestHandler<GetTicker24HQuery, Ticker24h?>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<Ticker24h?> Handle(GetTicker24HQuery request, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var key = $"Binance:Ticker24h:{request.Symbol}";

        // Try to get from cache
        var cachedData = await db.StringGetAsync(key);

        if (cachedData.HasValue)
        {
            logger.LogInformation("Returning cached 24h ticker for key {Key}", key);
            return JsonSerializer.Deserialize<Ticker24h>(cachedData.ToString());
        }

        logger.LogInformation("No cached 24h ticker found for key {Key}, fetching from Binance", key);

        // Fetch from service
        var ticker = await binanceService.GetTicker24hAsync(request.Symbol, cancellationToken);

        // Store in cache if result exists
        if (ticker != null)
        {
            var serialized = JsonSerializer.Serialize(ticker);
            await db.StringSetAsync(key, serialized, CacheDuration);

            logger.LogInformation("Stored 24h ticker in cache for key {Key}", key);
        }

        return ticker;
    }
}
