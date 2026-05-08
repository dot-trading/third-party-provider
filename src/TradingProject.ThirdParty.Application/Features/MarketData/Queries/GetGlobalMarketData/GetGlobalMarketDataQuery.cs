using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetGlobalMarketData;

public record GetGlobalMarketDataQuery : IRequest<GlobalMarketData?>;

public class GetGlobalMarketDataQueryHandler(
    ICoinGeckoService coinGeckoService,
    ICacheService cache,
    ILogger<GetGlobalMarketDataQueryHandler> logger) : IRequestHandler<GetGlobalMarketDataQuery, GlobalMarketData?>
{
    public async Task<GlobalMarketData?> Handle(GetGlobalMarketDataQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.CoinGecko.GlobalKey;

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached global market data");
            return JsonSerializer.Deserialize<GlobalMarketData>(cached);
        }

        logger.LogInformation("Fetching global market data from CoinGecko");
        var data = await coinGeckoService.GetGlobalDataAsync(cancellationToken);

        if (data is not null)
            await cache.SetAsync(key, JsonSerializer.Serialize(data), CacheKeys.CoinGecko.GlobalDuration, cancellationToken);

        return data;
    }
}
