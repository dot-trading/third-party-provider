using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class RssNewsSettings
{
    /// <summary>
    /// RSS feed URLs to aggregate. Each entry is a public, unauthenticated feed URL.
    /// Populated from configuration; falls back to built-in defaults if the section is absent.
    /// </summary>
    [Required]
    public List<string> FeedUrls { get; set; } =
    [
        "https://www.coindesk.com/arc/outboundfeeds/rss/",
        "https://cointelegraph.com/rss",
    ];
}
