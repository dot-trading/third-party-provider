using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using TradingProject.ThirdParty.Application.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;

public record GetMinNotionalQuery(string Symbol) : IRequest<double>;

public class GetMinNotionalQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetMinNotionalQueryHandler> logger) : IRequestHandler<GetMinNotionalQuery, double>
{
    private const string KeyPrefix = "Binance:MinNotional:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<double> Handle(GetMinNotionalQuery request, CancellationToken cancellationToken)
    {
        var key = $"{KeyPrefix}{request.Symbol}";

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached min notional for {Symbol}", request.Symbol);
            return double.Parse(cached, CultureInfo.InvariantCulture);
        }

        logger.LogInformation("Fetching min notional for {Symbol} from Binance", request.Symbol);
        var minNotional = await binanceService.GetMinNotionalAsync(request.Symbol, cancellationToken);

        if (minNotional > 0)
            await cache.SetAsync(key, minNotional.ToString(CultureInfo.InvariantCulture), CacheDuration, cancellationToken);

        return minNotional;
    }
}
