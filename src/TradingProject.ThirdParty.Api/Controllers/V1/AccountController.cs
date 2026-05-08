using MediatR;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using TradingProject.ThirdParty.Application.Features.Binance.Queries.GetBalances;

namespace TradingProject.ThirdParty.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AccountController(IMediator mediator) : ControllerBase
{
    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBalancesQuery(), cancellationToken);
        return Ok(result);
    }
}
