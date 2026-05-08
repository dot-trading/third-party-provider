using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetTrendingCoins;

public record GetTrendingCoinsQuery : IRequest<TrendingCoin[]>;

public class GetTrendingCoinsQueryHandler(
    ICoinGeckoService coinGeckoService,
    ICacheService cache,
    ILogger<GetTrendingCoinsQueryHandler> logger) : IRequestHandler<GetTrendingCoinsQuery, TrendingCoin[]>
{
    public async Task<TrendingCoin[]> Handle(GetTrendingCoinsQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.CoinGecko.TrendingKey;

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached trending coins");
            return JsonSerializer.Deserialize<TrendingCoin[]>(cached) ?? [];
        }

        logger.LogInformation("Fetching trending coins from CoinGecko");
        var coins = await coinGeckoService.GetTrendingCoinsAsync(cancellationToken);

        if (coins.Length > 0)
            await cache.SetAsync(key, JsonSerializer.Serialize(coins), CacheKeys.CoinGecko.TrendingDuration, cancellationToken);

        return coins;
    }
}
