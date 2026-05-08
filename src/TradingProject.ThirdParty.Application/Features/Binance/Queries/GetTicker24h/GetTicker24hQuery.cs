using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTicker24h;

public record GetTicker24hQuery(string Symbol) : IRequest<Ticker24h?>;

public class GetTicker24hQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetTicker24hQueryHandler> logger) : IRequestHandler<GetTicker24hQuery, Ticker24h?>
{
    public async Task<Ticker24h?> Handle(GetTicker24hQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Binance.Ticker24H(request.Symbol);

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached 24h ticker for key {Key}", key);
            return JsonSerializer.Deserialize<Ticker24h>(cached);
        }

        logger.LogInformation("Fetching 24h ticker from Binance for key {Key}", key);
        var result = await binanceService.GetTicker24HAsync(request.Symbol, cancellationToken);

        if (result is null) return null;

        var ticker = new Ticker24h(result.Symbol, result.LastPrice, result.PriceChangePercent, result.QuoteVolume, result.HighPrice, result.LowPrice);
        await cache.SetAsync(key, JsonSerializer.Serialize(ticker), CacheKeys.Binance.Ticker24HDuration, cancellationToken);

        return ticker;
    }
}
