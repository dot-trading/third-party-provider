using MediatR;
using Microsoft.AspNetCore.Mvc;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetKlines;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTicker24h;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;
using TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;

namespace TradingProject.ThirdParty.Api.Controllers;

[ApiController]
[Route("api/market-data")]
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
        var result = await mediator.Send(new GetTicker24HQuery(symbol), cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("sentiment/fear-and-greed")]
    public async Task<IActionResult> GetFearAndGreed(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetFearAndGreedQuery(), cancellationToken);
        return Ok(result);
    }
}
