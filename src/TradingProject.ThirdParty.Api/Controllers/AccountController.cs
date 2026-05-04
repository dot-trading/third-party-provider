using MediatR;
using Microsoft.AspNetCore.Mvc;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;

namespace TradingProject.ThirdParty.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(IMediator mediator) : ControllerBase
{
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBalancesQuery(), cancellationToken);
        return Ok(result);
    }
}
