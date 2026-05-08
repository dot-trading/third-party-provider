using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Constants;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;

public record GetPriceQuery(string Symbol) : IRequest<BinancePriceDto?>;

public class GetPriceQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<GetPriceQueryHandler> logger) : IRequestHandler<GetPriceQuery, BinancePriceDto?>
{
    public async Task<BinancePriceDto?> Handle(
        GetPriceQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Binance.Price(request.Symbol);

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached price for key {Key}", key);
            return JsonSerializer.Deserialize<BinancePriceDto>( cached, jsonSerializerOptions );
        }

        logger.LogInformation("Fetching price from Binance for key {Key}", key);
        var priceDto = await binanceService.GetCurrentPriceAsync(request.Symbol, cancellationToken);
        if (priceDto is not null)
        {
            await cache.SetAsync(
                key,
                JsonSerializer.Serialize(priceDto, jsonSerializerOptions),
                CacheKeys.Binance.PriceDuration,
                cancellationToken);
        }

        return priceDto;
    }
}