using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTicker24h;

public record GetTicker24hQuery(string Symbol) : IRequest<Ticker24h?>;

public class GetTicker24hQueryHandler(
    IBinanceService binanceService,
    ICacheService cache,
    ILogger<GetTicker24hQueryHandler> logger) : IRequestHandler<GetTicker24hQuery, Ticker24h?>
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public async Task<Ticker24h?> Handle(GetTicker24hQuery request, CancellationToken cancellationToken)
    {
        var key = $"Binance:Ticker24h:{request.Symbol}";

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached 24h ticker for key {Key}", key);
            return JsonSerializer.Deserialize<Ticker24h>(cached);
        }

        logger.LogInformation("Fetching 24h ticker from Binance for key {Key}", key);
        var ticker = await binanceService.GetTicker24hAsync(request.Symbol, cancellationToken);

        if (ticker is not null)
            await cache.SetAsync(key, JsonSerializer.Serialize(ticker), CacheDuration, cancellationToken);

        return ticker;
    }
}
