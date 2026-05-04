using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;

public record GetMinNotionalQuery(string Symbol) : IRequest<double>;

public class GetMinNotionalQueryHandler(
    IBinanceService binanceService,
    IConnectionMultiplexer redis,
    ILogger<GetMinNotionalQueryHandler> logger) : IRequestHandler<GetMinNotionalQuery, double>
{
    private const string KeyPrefix = "Binance:MinNotional:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    public async Task<double> Handle(GetMinNotionalQuery request, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var key = $"{KeyPrefix}{request.Symbol}";

        var cached = await db.StringGetAsync(key);
        if (cached.HasValue)
        {
            logger.LogInformation("Returning cached min notional for {Symbol}", request.Symbol);
            return double.Parse(cached.ToString());
        }

        logger.LogInformation("Fetching fresh min notional for {Symbol} from Binance", request.Symbol);
        var minNotional = await binanceService.GetMinNotionalAsync(request.Symbol, cancellationToken);

        if (minNotional > 0)
        {
            await db.StringSetAsync(key, minNotional.ToString(), CacheDuration);
        }

        return minNotional;
    }
}
