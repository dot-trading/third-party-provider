using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TradingProject.ThirdParty.Api.Controllers.V1;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Enums;
using TradingProject.ThirdParty.Domain.Models.Ai;
using Xunit;

namespace TradingProject.ThirdParty.Api.Tests.Controllers.V1;

public class AgentIAControllerTests
{
    private readonly Mock<IAgentIAService> _serviceMock;
    private readonly AgentIAController _controller;

    public AgentIAControllerTests()
    {
        _serviceMock = new Mock<IAgentIAService>();
        _controller = new AgentIAController(_serviceMock.Object);
    }

    [Fact]
    public async Task Invoke_WithGeminiFree_ShouldReturnOkWithSuccessResponse()
    {
        // Arrange
        var expectedResponse = new AiResponseDto
        {
            Content = "Hello from Gemini Free!",
            IsSuccess = true,
            ServiceType = "Gemini",
            PlanType = "Free"
        };

        _serviceMock
            .Setup(s => s.GenerateContentAsync(
                It.IsAny<string>(),
                ServiceType.Gemini,
                PlanType.Free,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new AiRequest { Prompt = "Hello" };

        // Act
        var result = await _controller.Invoke(
            ServiceType.Gemini,
            PlanType.Free,
            request,
            CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AiResponseDto>().Subject;
        response.Content.Should().Be("Hello from Gemini Free!");
        response.IsSuccess.Should().BeTrue();
        response.ServiceType.Should().Be("Gemini");
        response.PlanType.Should().Be("Free");
        _serviceMock.Verify(
            s => s.GenerateContentAsync("Hello", ServiceType.Gemini, PlanType.Free, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_WithGeminiPaid_ShouldReturnOkWithSuccessResponse()
    {
        // Arrange
        var expectedResponse = new AiResponseDto
        {
            Content = "Hello from Gemini Paid!",
            IsSuccess = true,
            ServiceType = "Gemini",
            PlanType = "Paid"
        };

        _serviceMock
            .Setup(s => s.GenerateContentAsync(
                It.IsAny<string>(),
                ServiceType.Gemini,
                PlanType.Paid,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new AiRequest { Prompt = "Hello" };

        // Act
        var result = await _controller.Invoke(
            ServiceType.Gemini,
            PlanType.Paid,
            request,
            CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AiResponseDto>().Subject;
        response.Content.Should().Be("Hello from Gemini Paid!");
        response.PlanType.Should().Be("Paid");
        _serviceMock.Verify(
            s => s.GenerateContentAsync("Hello", ServiceType.Gemini, PlanType.Paid, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_WithGrokPaid_ShouldReturnOkWithSuccessResponse()
    {
        // Arrange
        var expectedResponse = new AiResponseDto
        {
            Content = "Hello from Grok!",
            IsSuccess = true,
            ServiceType = "Grok",
            PlanType = "Paid"
        };

        _serviceMock
            .Setup(s => s.GenerateContentAsync(
                It.IsAny<string>(),
                ServiceType.Grok,
                PlanType.Paid,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new AiRequest { Prompt = "Hello" };

        // Act
        var result = await _controller.Invoke(
            ServiceType.Grok,
            PlanType.Paid,
            request,
            CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AiResponseDto>().Subject;
        response.Content.Should().Be("Hello from Grok!");
        response.ServiceType.Should().Be("Grok");
        response.PlanType.Should().Be("Paid");
        _serviceMock.Verify(
            s => s.GenerateContentAsync("Hello", ServiceType.Grok, PlanType.Paid, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Invoke_WithErrorResponse_ShouldReturnOkWithErrorMessage()
    {
        // Arrange
        var errorResponse = new AiResponseDto
        {
            IsSuccess = false,
            ErrorMessage = "Gemini API error (403): API key not valid",
            ServiceType = "Gemini",
            PlanType = "Free"
        };

        _serviceMock
            .Setup(s => s.GenerateContentAsync(
                It.IsAny<string>(),
                ServiceType.Gemini,
                PlanType.Free,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResponse);

        var request = new AiRequest { Prompt = "Hello" };

        // Act
        var result = await _controller.Invoke(
            ServiceType.Gemini,
            PlanType.Free,
            request,
            CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AiResponseDto>().Subject;
        response.IsSuccess.Should().BeFalse();
        response.ErrorMessage.Should().Contain("API key not valid");
    }

    [Fact]
    public async Task InvokeWithFallback_WhenFreeSucceeds_ShouldReturnFreeResponse()
    {
        // Arrange
        var freeResponse = new AiResponseDto
        {
            Content = "Hello from Free",
            IsSuccess = true,
            ServiceType = "Gemini",
            PlanType = "Free"
        };

        _serviceMock
            .Setup(s => s.GenerateContentWithGeminiFallbackAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(freeResponse);

        var request = new AiRequest { Prompt = "Hello" };

        // Act
        var result = await _controller.InvokeWithFallback(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AiResponseDto>().Subject;
        response.Content.Should().Be("Hello from Free");
        response.IsSuccess.Should().BeTrue();
        response.PlanType.Should().Be("Free");
        _serviceMock.Verify(
            s => s.GenerateContentWithGeminiFallbackAsync("Hello", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeWithFallback_WhenFreeFails_ShouldFallbackToPaid()
    {
        // Arrange
        var paidResponse = new AiResponseDto
        {
            Content = "Hello from Paid fallback",
            IsSuccess = true,
            ServiceType = "Gemini",
            PlanType = "Paid"
        };

        _serviceMock
            .Setup(s => s.GenerateContentWithGeminiFallbackAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidResponse);

        var request = new AiRequest { Prompt = "Hello" };

        // Act
        var result = await _controller.InvokeWithFallback(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AiResponseDto>().Subject;
        response.Content.Should().Be("Hello from Paid fallback");
        response.PlanType.Should().Be("Paid");
    }

    [Fact]
    public async Task Invoke_ShouldPassPromptToService()
    {
        // Arrange
        const string testPrompt = "What is the meaning of life?";

        _serviceMock
            .Setup(s => s.GenerateContentAsync(
                It.IsAny<string>(),
                It.IsAny<ServiceType>(),
                It.IsAny<PlanType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiResponseDto
            {
                Content = "42",
                IsSuccess = true
            });

        var request = new AiRequest { Prompt = testPrompt };

        // Act
        await _controller.Invoke(
            ServiceType.Gemini,
            PlanType.Free,
            request,
            CancellationToken.None);

        // Assert
        _serviceMock.Verify(
            s => s.GenerateContentAsync(testPrompt, ServiceType.Gemini, PlanType.Free, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
