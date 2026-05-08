using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.News;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Infrastructure.Services;

/// <summary>
/// Aggregates crypto news from public RSS 2.0 feeds — no API key required.
///
/// Strategy when currencies are specified:
///   1. For each known symbol, fetch the CryptoPanic per-coin RSS feed
///      (https://cryptopanic.com/news/{slug}/rss/) — articles are pre-filtered at source.
///   2. Also fetch the configured general feeds and filter their articles by symbol in title.
///   3. Merge, deduplicate by URL, sort descending by date, take limit.
///
/// When no currencies are specified, only configured general feeds are fetched.
/// Individual feed failures are silenced so the remaining feeds still contribute.
/// </summary>
public class RssNewsService(
    IHttpClientFactory httpClientFactory,
    IOptions<RssNewsSettings> settings) : INewsService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(HttpClientNames.RssNews);
    private readonly IReadOnlyList<string> _feedUrls = settings.Value.FeedUrls;

    // RFC 822 date formats used by RSS 2.0 feeds.
    private static readonly string[] RssDateFormats =
    [
        "ddd, dd MMM yyyy HH:mm:ss zzz",
        "ddd, dd MMM yyyy HH:mm:ss z",
        "ddd,  d MMM yyyy HH:mm:ss zzz",
        "dd MMM yyyy HH:mm:ss zzz",
    ];

    // Maps uppercase ticker symbols to CryptoPanic coin slugs.
    // Slug = the path segment used in https://cryptopanic.com/news/{slug}/rss/
    private static readonly IReadOnlyDictionary<string, string> CryptoPanicSlugs =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BTC"]   = "bitcoin",
            ["ETH"]   = "ethereum",
            ["BNB"]   = "binancecoin",
            ["SOL"]   = "solana",
            ["XRP"]   = "xrp",
            ["ADA"]   = "cardano",
            ["AVAX"]  = "avalanche",
            ["DOT"]   = "polkadot",
            ["NEAR"]  = "near-protocol",
            ["MATIC"] = "polygon",
            ["LINK"]  = "chainlink",
            ["UNI"]   = "uniswap",
            ["AAVE"]  = "aave",
            ["CRV"]   = "curve-dao-token",
            ["OP"]    = "optimism",
            ["ARB"]   = "arbitrum",
            ["DOGE"]  = "dogecoin",
            ["SHIB"]  = "shiba-inu",
            ["PEPE"]  = "pepe",
            ["FLOKI"] = "floki",
            ["WLD"]   = "worldcoin",
            ["LTC"]   = "litecoin",
            ["BCH"]   = "bitcoin-cash",
            ["TON"]   = "toncoin",
            ["SUI"]   = "sui",
            ["APT"]   = "aptos",
        };

    public async Task<NewsItem[]> GetNewsAsync(
        IEnumerable<string> currencies,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var symbols = currencies.Select(c => c.ToUpper()).ToArray();

        // Per-symbol CryptoPanic feeds — articles are already coin-specific, no title filter needed.
        var cryptoPanicTasks = symbols
            .Where(s => CryptoPanicSlugs.ContainsKey(s))
            .Select(s => FetchFeedAsync(
                $"https://cryptopanic.com/news/{CryptoPanicSlugs[s]}/rss/",
                tagCurrencies: [s],
                cancellationToken));

        // Configured general feeds — applied for all requests; title-filtered when symbols are set.
        var generalTasks = _feedUrls
            .Select(url => FetchFeedAsync(url, tagCurrencies: null, cancellationToken));

        var allItems = (await Task.WhenAll(cryptoPanicTasks.Concat(generalTasks)))
            .SelectMany(items => items);

        // CryptoPanic per-symbol items (Currencies.Length > 0) are already relevant.
        // General feed items are included only if their title mentions one of the requested symbols.
        var filtered = symbols.Length == 0
            ? allItems
            : allItems.Where(item =>
                item.Currencies.Length > 0
                || symbols.Any(s => item.Title.Contains(s, StringComparison.OrdinalIgnoreCase)));

        return filtered
            .DistinctBy(item => item.Url)
            .OrderByDescending(item => item.PublishedAt)
            .Take(limit)
            .ToArray();
    }

    private async Task<IEnumerable<NewsItem>> FetchFeedAsync(
        string url,
        string[]? tagCurrencies,
        CancellationToken cancellationToken)
    {
        try
        {
            var xml = await _httpClient.GetStringAsync(url, cancellationToken);
            return ParseRss(xml, url, tagCurrencies ?? []);
        }
        catch
        {
            return [];
        }
    }

    private static IEnumerable<NewsItem> ParseRss(string xml, string feedUrl, string[] currencies)
    {
        var doc = XDocument.Parse(xml);

        var channelTitle = doc.Descendants("channel").FirstOrDefault()?.Element("title")?.Value
            ?? ExtractDomainName(feedUrl);

        return doc.Descendants("item").Select(item =>
        {
            var title  = item.Element("title")?.Value   ?? string.Empty;
            var link   = item.Element("link")?.Value    ?? string.Empty;
            var source = item.Element("source")?.Value  ?? channelTitle;
            var pubDateRaw = item.Element("pubDate")?.Value ?? string.Empty;

            return new NewsItem(
                Title: title,
                Url: link,
                Source: source,
                PublishedAt: TryParseRssDate(pubDateRaw),
                Currencies: currencies,
                BullishVotes: 0,
                BearishVotes: 0);
        });
    }

    private static DateTime TryParseRssDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DateTime.UtcNow;

        if (DateTimeOffset.TryParseExact(raw.Trim(), RssDateFormats,
                CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dto))
            return dto.UtcDateTime;

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
            return fallback.UtcDateTime;

        return DateTime.UtcNow;
    }

    private static string ExtractDomainName(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return uri.Host;
        return url;
    }
}
