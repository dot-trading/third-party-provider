namespace TradingProject.ThirdParty.Domain.Models.News;

/// <summary>
/// A crypto news article aggregated by CryptoPanic.
/// BullishVotes and BearishVotes reflect community sentiment on the article.
/// </summary>
public record NewsItem(
    string Title,
    string Url,
    string Source,
    DateTime PublishedAt,
    string[] Currencies,
    int BullishVotes,
    int BearishVotes);
