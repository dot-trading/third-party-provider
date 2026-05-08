using MediatR;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetKlines;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTicker24h;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;
using TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;
using TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetCoinGeckoPrice;
using TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetGlobalMarketData;
using TradingProject.ThirdParty.Application.Features.MarketData.Queries.GetTrendingCoins;

namespace TradingProject.ThirdParty.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/market-data")]
public class MarketDataController(IMediator mediator) : ControllerBase
{
    [HttpGet("price/{symbol}")]
    public async Task<IActionResult> GetPrice(string symbol, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPriceQuery(symbol), cancellationToken);
        return Ok(result);
    }

    [HttpGet("notional/{symbol}")]
    public async Task<IActionResult> GetMinNotional(string symbol, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMinNotionalQuery(symbol), cancellationToken);
        return Ok(result);
    }

    [HttpGet("klines/{symbol}")]
    public async Task<IActionResult> GetKlines(string symbol, [FromQuery] string interval = "1h", [FromQuery] int limit = 24, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetKlinesQuery(symbol, interval, limit), cancellationToken);
        return Ok(result);
    }

    [HttpGet("ticker/{symbol}")]
    public async Task<IActionResult> GetTicker24h(string symbol, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTicker24hQuery(symbol), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("sentiment/fear-and-greed")]
    public async Task<IActionResult> GetFearAndGreed(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetFearAndGreedQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("price/coingecko/{coinId}")]
    public async Task<IActionResult> GetCoinGeckoPrice(string coinId, [FromQuery] string vsCurrency = "usd", CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetCoinGeckoPriceQuery(coinId, vsCurrency), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns aggregated global crypto market data: BTC/ETH dominance, total market cap,
    /// 24h volume and market cap change. Cached 5 minutes. Source: CoinGecko /global.
    /// </summary>
    [HttpGet("global")]
    public async Task<IActionResult> GetGlobalMarketData(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetGlobalMarketDataQuery(), cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Returns the top trending coins on CoinGecko by search volume over the last 24 hours.
    /// Cached 1 hour. Source: CoinGecko /search/trending.
    /// </summary>
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrendingCoins(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTrendingCoinsQuery(), cancellationToken);
        return Ok(result);
    }
}
