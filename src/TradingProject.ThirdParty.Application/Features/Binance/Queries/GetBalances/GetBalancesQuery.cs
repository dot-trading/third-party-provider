using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;

public record GetBalancesQuery : IRequest<Dictionary<string, double>>;

public class GetBalancesQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetBalancesQueryHandler> logger) : IRequestHandler<GetBalancesQuery, Dictionary<string, double>>
{
    private const string Key = "Binance:GetBalances";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public async Task<Dictionary<string, double>> Handle(GetBalancesQuery request, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(Key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached balances for key {Key}", Key);
            return JsonSerializer.Deserialize<Dictionary<string, double>>(cached) ?? [];
        }

        logger.LogInformation("Fetching balances from Binance for key {Key}", Key);
        var balances = await binanceService.GetBalancesAsync(cancellationToken);

        await cache.SetAsync(Key, JsonSerializer.Serialize(balances), CacheDuration, cancellationToken);

        return balances;
    }
}
