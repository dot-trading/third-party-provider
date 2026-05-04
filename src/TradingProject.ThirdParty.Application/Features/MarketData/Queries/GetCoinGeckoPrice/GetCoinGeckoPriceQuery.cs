using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetCoinGeckoPrice;

public record GetCoinGeckoPriceQuery(string CoinId, string VsCurrency = "usd") : IRequest<double>;

public class GetCoinGeckoPriceQueryHandler(
    ICoinGeckoService coinGeckoService,
    IConnectionMultiplexer redis,
    ILogger<GetCoinGeckoPriceQueryHandler> logger) : IRequestHandler<GetCoinGeckoPriceQuery, double>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<double> Handle(GetCoinGeckoPriceQuery request, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var key = $"CoinGecko:Price:{request.CoinId.ToLower()}:{request.VsCurrency.ToLower()}";

        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            logger.LogInformation("Returning cached CoinGecko price for {CoinId}", request.CoinId);
            return double.Parse(cached.ToString());
        }

        logger.LogInformation("Fetching fresh CoinGecko price for {CoinId}", request.CoinId);
        var price = await coinGeckoService.GetPriceAsync(request.CoinId, request.VsCurrency, cancellationToken);

        if (price > 0)
        {
            await db.StringSetAsync(key, price.ToString(), CacheDuration);
        }

        return price;
    }
}
