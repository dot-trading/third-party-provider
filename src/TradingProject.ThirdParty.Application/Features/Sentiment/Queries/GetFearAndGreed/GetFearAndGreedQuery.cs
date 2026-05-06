using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;

public record GetFearAndGreedQuery : IRequest<FearAndGreedIndex>;

public class GetFearAndGreedQueryHandler(
    ISentimentService sentimentService,
    ICacheService cache,
    ILogger<GetFearAndGreedQueryHandler> logger) : IRequestHandler<GetFearAndGreedQuery, FearAndGreedIndex>
{
    private const string Key = "Sentiment:FearAndGreed";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<FearAndGreedIndex> Handle(GetFearAndGreedQuery request, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(Key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached Fear & Greed Index");
            return JsonSerializer.Deserialize<FearAndGreedIndex>(cached)!;
        }

        logger.LogInformation("Fetching Fear & Greed Index from service");
        var index = await sentimentService.GetFearAndGreedIndexAsync(cancellationToken);

        await cache.SetAsync(Key, JsonSerializer.Serialize(index), CacheDuration, cancellationToken);

        return index;
    }
}
