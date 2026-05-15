using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTickers24h;

public record GetTickers24hQuery() : IRequest<List<Ticker24h>>;

public class GetTickers24hQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetTickers24hQueryHandler> logger) : IRequestHandler<GetTickers24hQuery, List<Ticker24h>>
{
    public async Task<List<Ticker24h>> Handle(GetTickers24hQuery request, CancellationToken cancellationToken)
    {
        var key = "Binance:Tickers24H:All";

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached bulk 24h tickers");
            return JsonSerializer.Deserialize<List<Ticker24h>>(cached) ?? [];
        }

        logger.LogInformation("Fetching bulk 24h tickers from Binance");
        var result = await binanceService.GetTickers24HAsync(cancellationToken);

        var tickers = result.Select(r => new Ticker24h(r.Symbol, r.LastPrice, r.PriceChangePercent, r.QuoteVolume, r.HighPrice, r.LowPrice)).ToList();
        
        await cache.SetAsync(key, JsonSerializer.Serialize(tickers), CacheKeys.Binance.Ticker24HDuration, cancellationToken);

        return tickers;
    }
}
