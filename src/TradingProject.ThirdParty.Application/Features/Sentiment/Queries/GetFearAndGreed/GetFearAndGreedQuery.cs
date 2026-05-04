using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Features.Sentiment.Queries.GetFearAndGreed;

public record GetFearAndGreedQuery : IRequest<FearAndGreedIndex>;

public class GetFearAndGreedQueryHandler(
    ISentimentService sentimentService,
    IConnectionMultiplexer redis,
    ILogger<GetFearAndGreedQueryHandler> logger) : IRequestHandler<GetFearAndGreedQuery, FearAndGreedIndex>
{
    private const string Key = "Sentiment:FearAndGreed";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public async Task<FearAndGreedIndex> Handle(GetFearAndGreedQuery request, CancellationToken cancellationToken)
    {
        var db = redis.GetDatabase();
        var cached = await db.StringGetAsync(Key);

        if (cached.HasValue)
        {
            logger.LogInformation("Returning cached Fear & Greed Index");
            return JsonSerializer.Deserialize<FearAndGreedIndex>(cached.ToString())!;
        }

        logger.LogInformation("Fetching fresh Fear & Greed Index from service");
        var index = await sentimentService.GetFearAndGreedIndexAsync(cancellationToken);

        await db.StringSetAsync(Key, JsonSerializer.Serialize(index), CacheDuration);

        return index;
    }
}
