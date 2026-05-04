using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetCoinGeckoPrice;

public record GetCoinGeckoPriceQuery(string CoinId, string VsCurrency = "usd") : IRequest<double>;

public class GetCoinGeckoPriceQueryHandler(
    ICoinGeckoService coinGeckoService,
    ICacheService cache,
    ILogger<GetCoinGeckoPriceQueryHandler> logger) : IRequestHandler<GetCoinGeckoPriceQuery, double>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<double> Handle(GetCoinGeckoPriceQuery request, CancellationToken cancellationToken)
    {
        var key = $"CoinGecko:Price:{request.CoinId.ToLower()}:{request.VsCurrency.ToLower()}";

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached CoinGecko price for {CoinId}", request.CoinId);
            return double.Parse(cached, CultureInfo.InvariantCulture);
        }

        logger.LogInformation("Fetching CoinGecko price for {CoinId}", request.CoinId);
        var price = await coinGeckoService.GetPriceAsync(request.CoinId, request.VsCurrency, cancellationToken);

        if (price > 0)
            await cache.SetAsync(key, price.ToString(CultureInfo.InvariantCulture), CacheDuration, cancellationToken);

        return price;
    }
}
