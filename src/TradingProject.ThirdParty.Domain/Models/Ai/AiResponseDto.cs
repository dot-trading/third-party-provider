using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Domain.Models.Ai;

public class AiResponseDto
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("serviceType")]
    public string ServiceType { get; set; } = string.Empty;

    [JsonPropertyName("planType")]
    public string PlanType { get; set; } = string.Empty;
}
