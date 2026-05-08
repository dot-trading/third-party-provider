using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetGlobalMarketData;

public record GetGlobalMarketDataQuery : IRequest<GlobalMarketData?>;

public class GetGlobalMarketDataQueryHandler(
    ICoinGeckoService coinGeckoService,
    ICacheService cache,
    ILogger<GetGlobalMarketDataQueryHandler> logger) : IRequestHandler<GetGlobalMarketDataQuery, GlobalMarketData?>
{
    private const string Key = "CoinGecko:Global";

    // CoinGecko updates global data every few minutes; 5-minute cache stays well under free-tier limits.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<GlobalMarketData?> Handle(GetGlobalMarketDataQuery request, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(Key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached global market data");
            return JsonSerializer.Deserialize<GlobalMarketData>(cached);
        }

        logger.LogInformation("Fetching global market data from CoinGecko");
        var data = await coinGeckoService.GetGlobalDataAsync(cancellationToken);

        if (data is not null)
            await cache.SetAsync(Key, JsonSerializer.Serialize(data), CacheDuration, cancellationToken);

        return data;
    }
}
