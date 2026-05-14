using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Enums;
using TradingProject.ThirdParty.Domain.Models.Ai;
using TradingProject.ThirdParty.Infrastructure.Services;
using TradingProject.ThirdParty.Infrastructure.Settings;
using Xunit;

namespace TradingProject.ThirdParty.Application.Tests.Infrastructure.Services;

public class AgentIAServiceTests : IDisposable
{
    private const string GeminiBaseUrl = "https://generativelanguage.googleapis.com";
    private const string GeminiFreeApiKey = "test-gemini-free-key";
    private const string GeminiPaidApiKey = "test-gemini-paid-key";
    private const string GrokBaseUrl = "https://api.x.ai";
    private const string GrokApiKey = "test-grok-key";

    private readonly Mock<HttpMessageHandler> _handlerMock = new();
    private readonly IAgentIAService _sut;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<AgentIAService>> _loggerMock = new();

    public AgentIAServiceTests()
    {
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri(GeminiBaseUrl)
        };

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();

        // Return the same test HttpClient for both named clients
        httpClientFactoryMock
            .Setup(f => f.CreateClient(HttpClientNames.Gemini))
            .Returns(_httpClient);
        httpClientFactoryMock
            .Setup(f => f.CreateClient(HttpClientNames.XAi))
            .Returns(_httpClient);

        var geminiSettings = Options.Create(new GeminiSettings
        {
            BaseUrl = GeminiBaseUrl,
            FreeApiKey = GeminiFreeApiKey,
            PaidApiKey = GeminiPaidApiKey,
            Model = "gemini-2.5-flash"
        });

        var grokSettings = Options.Create(new GrokSettings
        {
            BaseUrl = GrokBaseUrl,
            ApiKey = GrokApiKey,
            Model = "grok-3"
        });

        _sut = new AgentIAService(
            httpClientFactoryMock.Object,
            geminiSettings,
            grokSettings,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    // ========================================================================
    // Gemini Free
    // ========================================================================

    [Fact]
    public async Task GenerateContentAsync_WithGeminiFree_ShouldReturnSuccessResponse()
    {
        // Arrange
        const string prompt = "Hello Gemini Free!";
        const string expectedContent = "Hello from Gemini Free!";

        SetupGeminiResponse(expectedContent);

        // Act
        var result = await _sut.GenerateContentAsync(prompt, ServiceType.Gemini, PlanType.Free);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be(expectedContent);
        result.ServiceType.Should().Be("Gemini");
        result.PlanType.Should().Be("Free");
    }

    [Fact]
    public async Task GenerateContentAsync_WithGeminiFree_WhenApiReturnsError_ShouldReturnErrorResponse()
    {
        // Arrange
        const string prompt = "Hello";

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.ToString().Contains("generateContent")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent(
                    """{"error":{"message":"API key not valid."}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        // Act
        var result = await _sut.GenerateContentAsync(prompt, ServiceType.Gemini, PlanType.Free);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("403");
        result.ErrorMessage.Should().Contain("API key not valid");
        result.ServiceType.Should().Be("Gemini");
        result.PlanType.Should().Be("Free");
    }

    // ========================================================================
    // Gemini Paid
    // ========================================================================

    [Fact]
    public async Task GenerateContentAsync_WithGeminiPaid_ShouldUsePaidApiKey()
    {
        // Arrange
        const string prompt = "Hello Gemini Paid!";
        const string expectedContent = "Hello from Gemini Paid!";

        SetupGeminiResponse(expectedContent);

        // Act
        var result = await _sut.GenerateContentAsync(prompt, ServiceType.Gemini, PlanType.Paid);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be(expectedContent);
        result.PlanType.Should().Be("Paid");
    }

    [Fact]
    public async Task GenerateContentAsync_WithGeminiPaid_WhenApiKeyMissing_ShouldReturnMissingKeyError()
    {
        // Arrange
        var geminiSettings = Options.Create(new GeminiSettings
        {
            BaseUrl = GeminiBaseUrl,
            FreeApiKey = GeminiFreeApiKey,
            PaidApiKey = "", // Missing paid key
            Model = "gemini-2.5-flash"
        });

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

        var sut = new AgentIAService(
            httpClientFactoryMock.Object,
            geminiSettings,
            Options.Create(new GrokSettings { BaseUrl = GrokBaseUrl, ApiKey = GrokApiKey }),
            _loggerMock.Object);

        // Act
        var result = await sut.GenerateContentAsync("Hello", ServiceType.Gemini, PlanType.Paid);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("API key is not configured");
        result.ServiceType.Should().Be("Gemini");
        result.PlanType.Should().Be("Paid");
    }

    // ========================================================================
    // Grok
    // ========================================================================

    [Fact]
    public async Task GenerateContentAsync_WithGrok_ShouldReturnSuccessResponse()
    {
        // Arrange
        const string prompt = "Hello Grok!";
        const string expectedContent = "Hello from Grok!";
        var grokResponse = $$"""
            {
                "id": "chatcmpl-123",
                "object": "chat.completion",
                "choices": [
                    {
                        "message": {
                            "role": "assistant",
                            "content": "{{expectedContent}}"
                        }
                    }
                ]
            }
            """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.ToString().Contains("chat/completions")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(grokResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _sut.GenerateContentAsync(prompt, ServiceType.Grok, PlanType.Paid);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be(expectedContent);
        result.ServiceType.Should().Be("Grok");
        result.PlanType.Should().Be("Paid");
    }

    [Fact]
    public async Task GenerateContentAsync_WithGrok_WhenApiKeyMissing_ShouldReturnMissingKeyError()
    {
        // Arrange
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

        var sut = new AgentIAService(
            httpClientFactoryMock.Object,
            Options.Create(new GeminiSettings { BaseUrl = GeminiBaseUrl, FreeApiKey = GeminiFreeApiKey, PaidApiKey = GeminiPaidApiKey, Model = "gemini-2.5-flash" }),
            Options.Create(new GrokSettings { BaseUrl = GrokBaseUrl, ApiKey = "" }),
            _loggerMock.Object);

        // Act
        var result = await sut.GenerateContentAsync("Hello", ServiceType.Grok, PlanType.Paid);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("API key is not configured");
        result.ServiceType.Should().Be("Grok");
    }

    // ========================================================================
    // Unsupported service type
    // ========================================================================

    [Fact]
    public async Task GenerateContentAsync_WithUnsupportedServiceType_ShouldReturnError()
    {
        // Act
        var result = await _sut.GenerateContentAsync(
            "Hello",
            (ServiceType)999,
            PlanType.Free);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unsupported service type");
    }

    // ========================================================================
    // Gemini Fallback (Free → Paid)
    // ========================================================================

    [Fact]
    public async Task GenerateContentWithGeminiFallbackAsync_WhenFreeSucceeds_ShouldReturnFreeResult()
    {
        // Arrange
        const string prompt = "Hello";
        SetupGeminiResponse("Hello from Free");

        // Act
        var result = await _sut.GenerateContentWithGeminiFallbackAsync(prompt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be("Hello from Free");
        result.PlanType.Should().Be("Free");
    }

    [Fact]
    public async Task GenerateContentWithGeminiFallbackAsync_WhenFreeFails_ShouldFallbackToPaid()
    {
        // Arrange
        const string prompt = "Hello";

        // First call (Free) fails — we need to set up two sequential responses.
        // Use a queue to simulate the sequence: first request fails, second succeeds.
        var callCount = 0;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.ToString().Contains("generateContent")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Forbidden,
                        Content = new StringContent(
                            """{"error":{"message":"Rate limited"}}""",
                            Encoding.UTF8,
                            "application/json")
                    },
                    _ => new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent("""
                            {
                                "candidates": [{
                                    "content": {
                                        "parts": [{"text": "Hello from Paid fallback"}]
                                    }
                                }]
                            }
                            """, Encoding.UTF8, "application/json")
                    }
                };
            });

        // Act
        var result = await _sut.GenerateContentWithGeminiFallbackAsync(prompt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be("Hello from Paid fallback");
        result.PlanType.Should().Be("Paid");
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GenerateContentWithGeminiFallbackAsync_WhenBothFail_ShouldReturnPaidError()
    {
        // Arrange
        const string prompt = "Hello";

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.ToString().Contains("generateContent")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent(
                    """{"error":{"message":"Quota exceeded"}}""",
                    Encoding.UTF8,
                    "application/json")
            });

        // Act
        var result = await _sut.GenerateContentWithGeminiFallbackAsync(prompt);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("403");
        result.ErrorMessage.Should().Contain("Quota exceeded");
        result.PlanType.Should().Be("Paid");
    }

    [Fact]
    public async Task GenerateContentWithGeminiFallbackAsync_WhenFreeApiKeyMissing_ShouldFallbackToPaid()
    {
        // Arrange
        // Create a SUT with empty FreeApiKey
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

        SetupGeminiResponse("Hello from Paid fallback");

        var sut = new AgentIAService(
            httpClientFactoryMock.Object,
            Options.Create(new GeminiSettings
            {
                BaseUrl = GeminiBaseUrl,
                FreeApiKey = "", // missing free key
                PaidApiKey = GeminiPaidApiKey,
                Model = "gemini-2.5-flash"
            }),
            Options.Create(new GrokSettings { BaseUrl = GrokBaseUrl, ApiKey = GrokApiKey, Model = "grok-3" }),
            _loggerMock.Object);

        // Act
        var result = await sut.GenerateContentWithGeminiFallbackAsync("Hello");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be("Hello from Paid fallback");
        result.PlanType.Should().Be("Paid");
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    /// <summary>
    /// Sets up the mock HTTP handler to return a valid Gemini response.
    /// </summary>
    private void SetupGeminiResponse(string text)
    {
        var geminiResponse = $$"""
            {
                "candidates": [
                    {
                        "content": {
                            "parts": [
                                {
                                    "text": "{{text}}"
                                }
                            ]
                        }
                    }
                ]
            }
            """;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.RequestUri!.ToString().Contains("generateContent")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiResponse, Encoding.UTF8, "application/json")
            });
    }
}
