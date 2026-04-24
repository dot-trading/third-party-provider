using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;

public record GetPriceQuery(string Symbol) : IRequest<double>;

public class GetPriceQueryHandler(
    IBinanceService binanceService,
    IConnectionMultiplexer redis,
    ILogger<GetPriceQueryHandler> logger) : IRequestHandler<GetPriceQuery, double>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public async Task<double> Handle(GetPriceQuery request, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var key = $"Binance:Price:{request.Symbol}";

        // Try to get from cache
        var cachedData = await db.StringGetAsync(key);

        if (cachedData.HasValue)
        {
            logger.LogInformation("Returning cached price for key {Key}", key);
            return double.Parse(cachedData.ToString());
        }

        logger.LogInformation("No cached price found for key {Key}, fetching from Binance", key);

        // Fetch from service
        var price = await binanceService.GetCurrentPriceAsync(request.Symbol, cancellationToken);

        // Store in cache
        var serialized = price.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await db.StringSetAsync(key, serialized, CacheDuration);

        logger.LogInformation("Stored price in cache for key {Key}", key);

        return price;
    }
}
