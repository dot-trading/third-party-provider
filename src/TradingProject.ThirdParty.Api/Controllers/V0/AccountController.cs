using MediatR;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;

namespace TradingProject.ThirdParty.Api.Controllers.V0;

[ApiController]
[ApiVersion("0.0")]
[Route("api/v{version:apiVersion}/[controller]")]
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
