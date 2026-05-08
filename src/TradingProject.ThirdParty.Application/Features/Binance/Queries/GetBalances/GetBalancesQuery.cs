using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;

public record GetBalancesQuery : IRequest<Dictionary<string, double>>;

public class GetBalancesQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetBalancesQueryHandler> logger) : IRequestHandler<GetBalancesQuery, Dictionary<string, double>>
{
    public async Task<Dictionary<string, double>> Handle(GetBalancesQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Binance.BalancesKey;

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached balances for key {Key}", key);
            return JsonSerializer.Deserialize<Dictionary<string, double>>(cached) ?? [];
        }

        logger.LogInformation("Fetching balances from Binance for key {Key}", key);
        var balancesDto = await binanceService.GetBalancesAsync(cancellationToken);
        var balances = balancesDto?.Balances.ToDictionary(b => b.Asset, b => b.Free) ?? [];

        await cache.SetAsync(key, JsonSerializer.Serialize(balances), CacheKeys.Binance.BalancesDuration, cancellationToken);

        return balances;
    }
}
