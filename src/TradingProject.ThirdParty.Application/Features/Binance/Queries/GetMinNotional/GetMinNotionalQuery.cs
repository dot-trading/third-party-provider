using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;

public record GetMinNotionalQuery(string Symbol) : IRequest<double>;

public class GetMinNotionalQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetMinNotionalQueryHandler> logger) : IRequestHandler<GetMinNotionalQuery, double>
{
    public async Task<double> Handle(GetMinNotionalQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Binance.MinNotional(request.Symbol);

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached min notional for {Symbol}", request.Symbol);
            return double.Parse(cached, CultureInfo.InvariantCulture);
        }

        logger.LogInformation("Fetching min notional for {Symbol} from Binance", request.Symbol);
        var result = await binanceService.GetMinNotionalAsync(request.Symbol, cancellationToken);
        var minNotional = result ?? 0;

        if (minNotional > 0)
            await cache.SetAsync(key, minNotional.ToString(CultureInfo.InvariantCulture), CacheKeys.Binance.MinNotionalDuration, cancellationToken);

        return minNotional;
    }
}
