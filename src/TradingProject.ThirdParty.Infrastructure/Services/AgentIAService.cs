using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Enums;
using TradingProject.ThirdParty.Domain.Models.Ai;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class AgentIAService : IAgentIAService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<GeminiSettings> _geminiSettings;
    private readonly IOptions<GrokSettings> _grokSettings;
    private readonly ILogger<AgentIAService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AgentIAService(
        IHttpClientFactory httpClientFactory,
        IOptions<GeminiSettings> geminiSettings,
        IOptions<GrokSettings> grokSettings,
        ILogger<AgentIAService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _geminiSettings = geminiSettings;
        _grokSettings = grokSettings;
        _logger = logger;
    }

    public async Task<AiResponseDto> GenerateContentAsync(
        string prompt,
        ServiceType serviceType,
        PlanType planType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return serviceType switch
            {
                ServiceType.Gemini => await CallGeminiAsync(prompt, planType, cancellationToken),
                ServiceType.Grok => await CallGrokAsync(prompt, cancellationToken),
                _ => new AiResponseDto
                {
                    IsSuccess = false,
                    ErrorMessage = $"Unsupported service type: {serviceType}",
                    ServiceType = serviceType.ToString(),
                    PlanType = planType.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service call failed for {ServiceType}/{PlanType}", serviceType, planType);

            return new AiResponseDto
            {
                IsSuccess = false,
                ErrorMessage = $"AI service error: {ex.Message}",
                ServiceType = serviceType.ToString(),
                PlanType = planType.ToString()
            };
        }
    }

    public async Task<AiResponseDto> GenerateContentWithGeminiFallbackAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting Gemini Free, with fallback to Gemini Paid");

        var freeResult = await GenerateContentAsync(prompt, ServiceType.Gemini, PlanType.Free, cancellationToken);

        if (freeResult.IsSuccess)
            return freeResult;

        _logger.LogWarning(
            "Gemini Free failed ({Error}). Falling back to Gemini Paid.",
            freeResult.ErrorMessage);

        var paidResult = await GenerateContentAsync(prompt, ServiceType.Gemini, PlanType.Paid, cancellationToken);

        return paidResult;
    }

    private async Task<AiResponseDto> CallGeminiAsync(
        string prompt,
        PlanType planType,
        CancellationToken cancellationToken)
    {
        var settings = _geminiSettings.Value;
        var apiKey = planType switch
        {
            PlanType.Free => settings.FreeApiKey,
            PlanType.Paid => settings.PaidApiKey,
            _ => throw new ArgumentOutOfRangeException(nameof(planType), planType, null)
        };

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiResponseDto
            {
                IsSuccess = false,
                ErrorMessage = $"Gemini {planType} API key is not configured",
                ServiceType = nameof(ServiceType.Gemini),
                PlanType = planType.ToString()
            };
        }

        var client = _httpClientFactory.CreateClient(HttpClientNames.Gemini);

        var url = $"/v1beta/models/{settings.Model}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var response = await client.PostAsJsonAsync(url, requestBody, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new AiResponseDto
            {
                IsSuccess = false,
                ErrorMessage = $"Gemini API error ({(int)response.StatusCode}): {TruncateForLog(responseBody)}",
                ServiceType = nameof(ServiceType.Gemini),
                PlanType = planType.ToString()
            };
        }

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return new AiResponseDto
        {
            Content = text ?? string.Empty,
            IsSuccess = true,
            ServiceType = nameof(ServiceType.Gemini),
            PlanType = planType.ToString()
        };
    }

    private async Task<AiResponseDto> CallGrokAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        var settings = _grokSettings.Value;

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return new AiResponseDto
            {
                IsSuccess = false,
                ErrorMessage = "Grok API key is not configured",
                ServiceType = nameof(ServiceType.Grok),
                PlanType = nameof(PlanType.Paid)
            };
        }

        var client = _httpClientFactory.CreateClient(HttpClientNames.XAi);

        var requestBody = new
        {
            model = settings.Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = JsonContent.Create(requestBody, options: JsonOptions)
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ApiKey);

        var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new AiResponseDto
            {
                IsSuccess = false,
                ErrorMessage = $"Grok API error ({(int)response.StatusCode}): {TruncateForLog(responseBody)}",
                ServiceType = nameof(ServiceType.Grok),
                PlanType = nameof(PlanType.Paid)
            };
        }

        using var doc = JsonDocument.Parse(responseBody);
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return new AiResponseDto
        {
            Content = text ?? string.Empty,
            IsSuccess = true,
            ServiceType = nameof(ServiceType.Grok),
            PlanType = nameof(PlanType.Paid)
        };
    }

    private static string TruncateForLog(string message, int maxLength = 500)
    {
        return message.Length <= maxLength
            ? message
            : string.Concat(message.AsSpan(0, maxLength), "...");
    }
}
