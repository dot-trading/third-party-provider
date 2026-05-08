using MediatR;
using Microsoft.AspNetCore.Mvc;
using TradingProject.ThirdParty.Application.Features.News.Queries.GetNews;

namespace TradingProject.ThirdParty.Api.Controllers;

[ApiController]
[Route("api/news")]
public class NewsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns the latest crypto news articles from CryptoPanic.
    /// Results are cached for 15 minutes to stay within free-tier rate limits (5 req/min).
    /// </summary>
    /// <param name="currencies">Comma-separated currency symbols to filter by (e.g., BTC,ETH). Omit for general market news.</param>
    /// <param name="limit">Maximum number of articles to return (default: 10).</param>
    [HttpGet]
    public async Task<IActionResult> GetNews(
        [FromQuery] string? currencies,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var currencyList = string.IsNullOrWhiteSpace(currencies)
            ? []
            : currencies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var result = await mediator.Send(new GetNewsQuery(currencyList, limit), cancellationToken);
        return Ok(result);
    }
}
