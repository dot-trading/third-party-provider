using System.ComponentModel.DataAnnotations;

namespace TradingProject.ThirdParty.Infrastructure.Settings;

public class GeminiSettings
{
    [Required]
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";

    public string FreeApiKey { get; set; } = string.Empty;

    public string PaidApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gemini-2.5-flash";
}
