using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Enums;
using TradingProject.ThirdParty.Domain.Models.Ai;

namespace TradingProject.ThirdParty.Application.Tests.Stubs;

/// <summary>
/// A lightweight stub implementation of <see cref="IAgentIAService"/>
/// that returns predefined responses. Useful for controller-level tests
/// that don't need to exercise the actual HTTP call logic.
/// </summary>
public class StubAgentIAService : IAgentIAService
{
    private readonly AiResponseDto _response;
    private readonly Exception? _exception;

    /// <param name="response">The response to return from every invocation.</param>
    /// <param name="exception">Optional exception to throw instead of returning a response.</param>
    public StubAgentIAService(AiResponseDto response, Exception? exception = null)
    {
        _response = response;
        _exception = exception;
    }

    public Task<AiResponseDto> GenerateContentWithGeminiFallbackAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        var result = new AiResponseDto
        {
            Content = _response.Content,
            IsSuccess = _response.IsSuccess,
            ErrorMessage = _response.ErrorMessage,
            ServiceType = nameof(ServiceType.Gemini),
            PlanType = nameof(PlanType.Paid)
        };

        return Task.FromResult(result);
    }

    public Task<AiResponseDto> GenerateContentAsync(
        string prompt,
        ServiceType serviceType,
        PlanType planType,
        CancellationToken cancellationToken = default)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        // Copy the response and stamp it with the request parameters so tests can assert them.
        var result = new AiResponseDto
        {
            Content = _response.Content,
            IsSuccess = _response.IsSuccess,
            ErrorMessage = _response.ErrorMessage,
            ServiceType = serviceType.ToString(),
            PlanType = planType.ToString()
        };

        return Task.FromResult(result);
    }
}
