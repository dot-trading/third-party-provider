using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class BinanceSettings
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    
    [Required]
    public string ApiSecret { get; set; } = string.Empty;
    
    [Required, Url]
    public string BaseUrl { get; set; } = "https://api.binance.com";
}
