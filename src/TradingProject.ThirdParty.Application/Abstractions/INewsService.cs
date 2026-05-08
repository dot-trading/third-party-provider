using TradingProject.ThirdParty.Domain.Models.News;

namespace TradingProject.ThirdParty.Application.Abstractions;

/// <summary>
/// Aggregates crypto news articles from external sources.
/// Implementations should apply caching to avoid hammering rate-limited APIs.
/// </summary>
public interface INewsService
{
    /// <summary>
    /// Returns the latest news articles for the given currency symbols (e.g., "BTC", "ETH").
    /// Pass an empty collection to get general market news with no currency filter.
    /// </summary>
    Task<NewsItem[]> GetNewsAsync(
        IEnumerable<string> currencies,
        int limit = 10,
        CancellationToken cancellationToken = default);
}