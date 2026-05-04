using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;

public record GetPriceQuery(string Symbol) : IRequest<double>;

public class GetPriceQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetPriceQueryHandler> logger) : IRequestHandler<GetPriceQuery, double>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public async Task<double> Handle(GetPriceQuery request, CancellationToken cancellationToken)
    {
        var key = $"Binance:Price:{request.Symbol}";

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached price for key {Key}", key);
            return double.Parse(cached, CultureInfo.InvariantCulture);
        }

        logger.LogInformation("Fetching price from Binance for key {Key}", key);
        var price = await binanceService.GetCurrentPriceAsync(request.Symbol, cancellationToken);

        await cache.SetAsync(key, price.ToString(CultureInfo.InvariantCulture), CacheDuration, cancellationToken);

        return price;
    }
}
