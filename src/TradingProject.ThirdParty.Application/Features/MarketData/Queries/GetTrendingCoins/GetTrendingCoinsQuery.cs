using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetTrendingCoins;

public record GetTrendingCoinsQuery : IRequest<TrendingCoin[]>;

public class GetTrendingCoinsQueryHandler(
    ICoinGeckoService coinGeckoService,
    ICacheService cache,
    ILogger<GetTrendingCoinsQueryHandler> logger) : IRequestHandler<GetTrendingCoinsQuery, TrendingCoin[]>
{
    private const string Key = "CoinGecko:Trending";

    // Trending list changes slowly; 1-hour cache is sufficient and minimizes API calls.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<TrendingCoin[]> Handle(GetTrendingCoinsQuery request, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(Key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached trending coins");
            return JsonSerializer.Deserialize<TrendingCoin[]>(cached) ?? [];
        }

        logger.LogInformation("Fetching trending coins from CoinGecko");
        var coins = await coinGeckoService.GetTrendingCoinsAsync(cancellationToken);

        if (coins.Length > 0)
            await cache.SetAsync(Key, JsonSerializer.Serialize(coins), CacheDuration, cancellationToken);

        return coins;
    }
}
