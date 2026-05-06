using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Abstractions;

public interface ISentimentService
{
    Task<FearAndGreedIndex> GetFearAndGreedIndexAsync(CancellationToken cancellationToken = default);
}
