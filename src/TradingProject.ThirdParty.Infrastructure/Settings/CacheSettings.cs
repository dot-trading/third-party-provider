using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class CacheSettings
{
    /// <summary>Whether caching is enabled. When disabled, all cache calls become no-ops.</summary>
    [Required]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cache provider to use. Supported values: <c>"Redis"</c> or <c>"Memory"</c>.
    /// This property is case-insensitive.
    /// </summary>
    [Required]
    [RegularExpression("^(Redis|Memory)$", ErrorMessage = "Cache provider must be either 'Redis' or 'Memory'.")]
    public string Provider { get; set; } = "Redis";
}
