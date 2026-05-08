using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Client.Configuration;

/// <summary>
/// Configuration options for the <c>TradingProject.ThirdParty.Client</c>.
/// Bind from <c>IConfiguration</c> section <c>"ThirdPartyApi"</c>.
/// </summary>
public class ThirdPartyApiClientOptions
{
    /// <summary>Configuration section name in appsettings.json / environment variables.</summary>
    public const string SectionName = "ThirdPartyApi";

    /// <summary>
    /// Base URL of the Third-Party Provider API (e.g. <c>http://third-party-provider/api</c>).
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Optional API key sent as the <c>X-Api-Key</c> header.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Request timeout in seconds. Defaults to 30.
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 30;
}
