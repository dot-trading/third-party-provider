using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Constants;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;

public record GetMinNotionalQuery(string Symbol) : IRequest<BinanceFilterDto?>;

public class GetMinNotionalQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<GetMinNotionalQueryHandler> logger) : IRequestHandler<GetMinNotionalQuery, BinanceFilterDto?>
{
    public async Task<BinanceFilterDto?> Handle(GetMinNotionalQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Binance.MinNotional(request.Symbol);

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached min notional for {Symbol}", request.Symbol);
            return JsonSerializer.Deserialize<BinanceFilterDto>(cached, jsonSerializerOptions);
        }

        logger.LogInformation("Fetching min notional for {Symbol} from Binance", request.Symbol);
        var result = await binanceService.GetMinNotionalAsync(request.Symbol, cancellationToken);
        if (result is not null)
        {
            await cache.SetAsync(
                key,
                JsonSerializer.Serialize(result, jsonSerializerOptions),
                CacheKeys.Binance.MinNotionalDuration,
                cancellationToken);
        }

        return result;
    }
}