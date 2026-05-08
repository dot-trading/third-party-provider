using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Models.News;

namespace TradingProject.ThirdParty.Application.Features.News.Queries.GetNews;

/// <param name="Currencies">Currency symbols to filter by (e.g., "BTC", "ETH"). Empty means no filter.</param>
/// <param name="Limit">Maximum number of articles to return.</param>
public record GetNewsQuery(IEnumerable<string> Currencies, int Limit = 10) : IRequest<NewsItem[]>;

public class GetNewsQueryHandler(
    INewsService newsService,
    ICacheService cache,
    ILogger<GetNewsQueryHandler> logger) : IRequestHandler<GetNewsQuery, NewsItem[]>
{
    // 15-minute cache avoids redundant upstream calls while keeping news reasonably fresh.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public async Task<NewsItem[]> Handle(GetNewsQuery request, CancellationToken cancellationToken)
    {
        var currencyList = request.Currencies.Select(c => c.ToUpper()).OrderBy(c => c).ToArray();
        var key = $"News:{string.Join(",", currencyList)}:{request.Limit}";

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached news for {Currencies}", string.Join(",", currencyList));
            return JsonSerializer.Deserialize<NewsItem[]>(cached) ?? [];
        }

        logger.LogInformation("Fetching news for {Currencies}", string.Join(",", currencyList));
        var news = await newsService.GetNewsAsync(currencyList, request.Limit, cancellationToken);

        if (news.Length > 0)
            await cache.SetAsync(key, JsonSerializer.Serialize(news), CacheDuration, cancellationToken);

        return news;
    }
}
