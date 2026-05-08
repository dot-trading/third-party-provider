using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Constants;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetKlines;

public record GetKlinesQuery(string Symbol, string Interval = "1h", int Limit = 24) : IRequest<KLineDto[]>;

public class GetKlinesQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetKlinesQueryHandler> logger) : IRequestHandler<GetKlinesQuery, KLineDto[]>
{
    public async Task<KLineDto[]> Handle(GetKlinesQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Binance.Klines(request.Symbol, request.Interval, request.Limit);

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached klines for key {Key}", key);
            return JsonSerializer.Deserialize<KLineDto[]>(cached) ?? [];
        }

        logger.LogInformation("Fetching klines from Binance for key {Key}", key);
        var result = await binanceService.GetKLinesAsync(
            request.Symbol,
            request.Interval, request.Limit, cancellationToken);

        var klines = result.Select(k => new KLineDto(k.OpenTime, k.Open, k.High, k.Low, k.Close, k.Volume)).ToArray();

        await cache.SetAsync(key, JsonSerializer.Serialize(klines), CacheKeys.Binance.KlinesDuration, cancellationToken);

        return klines;
    }
}
