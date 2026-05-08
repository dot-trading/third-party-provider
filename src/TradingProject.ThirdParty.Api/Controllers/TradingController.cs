using MediatR;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketBuy;
using TradingProject.ThirdParty.Application.Features.Binance.Commands.PlaceMarketSell;

namespace TradingProject.ThirdParty.Api.Controllers;

[ApiController]
[ApiVersion("0.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class TradingController(IMediator mediator) : ControllerBase
{
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
