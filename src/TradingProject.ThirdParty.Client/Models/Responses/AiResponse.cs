using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Client.Models.Responses;

/// <summary>
/// Response from the AI service endpoint (AgentIA).
/// </summary>
public class AiResponse
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

/// <summary>
/// Request body for the AI service.
/// </summary>
public class AiServiceRequest
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;
}
