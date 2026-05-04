using MediatR;
using Microsoft.AspNetCore.Mvc;
using TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketBuy;
using TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketSell;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetKlines;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetTicker24h;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetMinNotional;

namespace TradingProject.ThirdParty.Api.Controllers;

[Obsolete("This controller is deprecated. Please use the specialized providers controllers instead.")]
[ApiController]
[Route("api/[controller]")]
public class BinanceController(IMediator mediator) : ControllerBase
{
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBalancesQuery(), cancellationToken);
        return Ok(result);
    }

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

    [HttpPost("order/buy")]
    public async Task<IActionResult> PlaceMarketBuy([FromBody] PlaceMarketBuyCommand command, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("order/sell")]
    public async Task<IActionResult> PlaceMarketSell([FromBody] PlaceMarketSellCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException ex) when (ex.Message.StartsWith("Binance order failed"))
        {
            return BadRequest(ex.Message);
        }
    }
}
