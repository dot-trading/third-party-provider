using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class GrokSettings
{
    [Required]
    public string BaseUrl { get; set; } = "https://api.x.ai";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "grok-3";
}
