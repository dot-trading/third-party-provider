using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;

namespace TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetCoinGeckoPrice;

public record GetCoinGeckoPriceQuery(string CoinId, string VsCurrency = "usd") : IRequest<double>;

public class GetCoinGeckoPriceQueryHandler(
    ICoinGeckoService coinGeckoService,
    ICacheService cache,
    ILogger<GetCoinGeckoPriceQueryHandler> logger) : IRequestHandler<GetCoinGeckoPriceQuery, double>
{
    public async Task<double> Handle(GetCoinGeckoPriceQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.CoinGecko.Price(request.CoinId, request.VsCurrency);

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached CoinGecko price for {CoinId}", request.CoinId);
            return double.Parse(cached, CultureInfo.InvariantCulture);
        }

        logger.LogInformation("Fetching CoinGecko price for {CoinId}", request.CoinId);
        var priceDto = await coinGeckoService.GetPriceAsync(request.CoinId, request.VsCurrency, cancellationToken);
        var price = priceDto?.Price ?? 0;

        if (price > 0)
            await cache.SetAsync(key, price.ToString(CultureInfo.InvariantCulture), CacheKeys.CoinGecko.PriceDuration, cancellationToken);

        return price;
    }
}
