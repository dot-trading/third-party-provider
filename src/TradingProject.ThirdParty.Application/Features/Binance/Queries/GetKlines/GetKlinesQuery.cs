using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetKlines;

public record GetKlinesQuery(string Symbol, string Interval = "1h", int Limit = 24) : IRequest<List<Kline>>;

public class GetKlinesQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetKlinesQueryHandler> logger) : IRequestHandler<GetKlinesQuery, List<Kline>>
{
    public async Task<List<Kline>> Handle(GetKlinesQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Binance.Klines(request.Symbol, request.Interval, request.Limit);

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached klines for key {Key}", key);
            return JsonSerializer.Deserialize<List<Kline>>(cached) ?? [];
        }

        logger.LogInformation("Fetching klines from Binance for key {Key}", key);
        var result = await binanceService.GetKLinesAsync(request.Symbol, request.Interval, request.Limit, cancellationToken);

        var klines = result.Select(k => new Kline(k.OpenTime, k.Open, k.High, k.Low, k.Close, k.Volume)).ToList();

        await cache.SetAsync(key, JsonSerializer.Serialize(klines), CacheKeys.Binance.KlinesDuration, cancellationToken);

        return klines;
    }
}
