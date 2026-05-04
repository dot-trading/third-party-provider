using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Domain.Abstractions;

public interface ISentimentService
{
    Task<FearAndGreedIndex> GetFearAndGreedIndexAsync(CancellationToken cancellationToken = default);
}
