using MediatR;
using Microsoft.AspNetCore.Mvc;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetPrice;

namespace TradingProject.ThirdParty.Api.Controllers;

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
}
