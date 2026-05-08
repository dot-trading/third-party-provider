using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Infrastructure.Services;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Application.Tests.Integration.News;

/// <summary>
/// Integration tests for <see cref="RssNewsService"/>.
/// These tests make real HTTP calls — they run only in Debug configuration.
///
/// Observable contract being tested:
///   - The service returns valid, non-empty news articles for known symbols.
///   - Per-symbol CryptoPanic feeds are used when available; general feeds are the fallback.
///   - Results are deduplicated, sorted by date descending, and capped to the requested limit.
/// </summary>
public class RssNewsServiceIntegrationTests
{
    private static RssNewsService BuildService(List<string>? feedUrls = null)
    {
        var settings = Options.Create(new RssNewsSettings
        {
            FeedUrls = feedUrls ??
            [
                "https://www.coindesk.com/arc/outboundfeeds/rss/",
                "https://cointelegraph.com/rss",
            ]
        });

        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(HttpClientNames.RssNews)).Returns(httpClient);

        return new RssNewsService(factory.Object, settings);
    }

    [DebugOnlyFact]
    public async Task GetNewsAsync_WithNoCurrencies_ReturnsNewsFromConfiguredFeeds()
    {
        var service = BuildService();

        var result = await service.GetNewsAsync([], limit: 10);

        result.Should().NotBeEmpty();
        result.Should().HaveCountLessOrEqualTo(10);
        result.Should().AllSatisfy(item =>
        {
            item.Title.Should().NotBeNullOrWhiteSpace();
            item.Url.Should().NotBeNullOrWhiteSpace();
            item.Source.Should().NotBeNullOrWhiteSpace();
            item.PublishedAt.Should().BeAfter(DateTime.UtcNow.AddDays(-30));
        });
        result.Should().BeInDescendingOrder(item => item.PublishedAt);
    }

    [DebugOnlyFact]
    public async Task GetNewsAsync_WithBtcSymbol_ReturnsRelevantArticles()
    {
        var service = BuildService();

        var result = await service.GetNewsAsync(["BTC"], limit: 5);

        // CryptoPanic per-coin feed tags items via Currencies; general feeds filter by title.
        // Either source is valid — the contract is that returned articles are BTC-relevant.
        result.Should().NotBeEmpty();
        result.Should().HaveCountLessOrEqualTo(5);
        result.Should().AllSatisfy(item =>
        {
            var isTagged = item.Currencies.Contains("BTC");
            var isTitleMatch = item.Title.Contains("BTC", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("Bitcoin", StringComparison.OrdinalIgnoreCase);
            (isTagged || isTitleMatch).Should().BeTrue(
                because: $"'{item.Title}' should be Bitcoin-related (tagged or title match)");
        });
    }

    [DebugOnlyFact]
    public async Task GetNewsAsync_WithEthSymbol_ReturnsRelevantArticles()
    {
        var service = BuildService();

        var result = await service.GetNewsAsync(["ETH"], limit: 5);

        result.Should().NotBeEmpty();
        result.Should().AllSatisfy(item =>
        {
            var isTagged = item.Currencies.Contains("ETH");
            var isTitleMatch = item.Title.Contains("ETH", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("Ethereum", StringComparison.OrdinalIgnoreCase);
            (isTagged || isTitleMatch).Should().BeTrue(
                because: $"'{item.Title}' should be Ethereum-related (tagged or title match)");
        });
    }

    [DebugOnlyFact]
    public async Task GetNewsAsync_WithMultipleCurrencies_MergesDeduplicatesAndSortsByDate()
    {
        var service = BuildService();

        var result = await service.GetNewsAsync(["BTC", "SOL"], limit: 10);

        result.Should().NotBeEmpty();
        result.Should().HaveCountLessOrEqualTo(10);

        // No duplicate URLs across merged feeds.
        result.Select(item => item.Url).Should().OnlyHaveUniqueItems();

        // Every article must be relevant to BTC or SOL (tagged or title).
        result.Should().AllSatisfy(item =>
        {
            var isRelevant =
                item.Currencies.Intersect(["BTC", "SOL"]).Any()
                || item.Title.Contains("BTC", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("Bitcoin", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("SOL", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("Solana", StringComparison.OrdinalIgnoreCase);
            isRelevant.Should().BeTrue(because: $"'{item.Title}' should be BTC or SOL related");
        });

        result.Should().BeInDescendingOrder(item => item.PublishedAt);
    }

    [DebugOnlyFact]
    public async Task GetNewsAsync_WithUnknownSymbol_ReturnsEmptyOrOnlyTitleMatches()
    {
        // "UNKNOWN" has no CryptoPanic slug — falls back to title filtering on general feeds.
        // An empty result is valid and expected behaviour.
        var service = BuildService();

        var result = await service.GetNewsAsync(["UNKNOWN"], limit: 5);

        result.Should().HaveCountLessOrEqualTo(5);

        // If any items are returned, they must match by title.
        foreach (var item in result)
        {
            item.Title.Should().ContainEquivalentOf("UNKNOWN",
                because: $"only title-matched articles should pass the filter");
        }
    }

    [DebugOnlyFact]
    public async Task GetNewsAsync_WhenOneFeedFails_StillReturnsResultsFromOtherFeeds()
    {
        var service = BuildService([
            "https://this-domain-does-not-exist-xyz.invalid/rss",
            "https://cointelegraph.com/rss",
        ]);

        var result = await service.GetNewsAsync([], limit: 5);

        result.Should().NotBeEmpty("the broken feed should be silenced and CoinTelegraph still contributes");
    }
}
