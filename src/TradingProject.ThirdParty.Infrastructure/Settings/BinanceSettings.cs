namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class BinanceSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.binance.com";
}
