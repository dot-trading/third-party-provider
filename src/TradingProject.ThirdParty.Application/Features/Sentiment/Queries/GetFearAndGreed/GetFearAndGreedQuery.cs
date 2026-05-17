using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;

public record GetFearAndGreedQuery : IRequest<FearAndGreedIndex?>;

public class GetFearAndGreedQueryHandler(
    ISentimentService sentimentService,
    ICacheService cache,
    JsonSerializerOptions options,
    ILogger<GetFearAndGreedQueryHandler> logger) : IRequestHandler<GetFearAndGreedQuery, FearAndGreedIndex?>
{
    public async Task<FearAndGreedIndex?> Handle(GetFearAndGreedQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Sentiment.FearAndGreedKey;

        var cached = await cache.GetAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Returning cached Fear & Greed Index");
            return JsonSerializer.Deserialize<FearAndGreedIndex>(cached, options);
        }

        logger.LogInformation("Fetching Fear & Greed Index from service");
        var index = await sentimentService.GetFearAndGreedIndexAsync(cancellationToken);

        await cache.SetAsync(key, JsonSerializer.Serialize(index), CacheKeys.Sentiment.FearAndGreedDuration, cancellationToken);

        return index;
    }
}
