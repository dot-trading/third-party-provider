using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Domain.Models.Ai;

public class AiRequest
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;
}
