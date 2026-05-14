using TradingProject.ThirdParty.Domain.Enums;
using TradingProject.ThirdParty.Domain.Models.Ai;

namespace TradingProject.ThirdParty.Application.Abstractions;

public interface IAgentIAService
{
    Task<AiResponseDto> GenerateContentAsync(
        string prompt,
        ServiceType serviceType,
        PlanType planType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries Gemini Free first. If it fails, falls back to Gemini Paid.
    /// </summary>
    Task<AiResponseDto> GenerateContentWithGeminiFallbackAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}
