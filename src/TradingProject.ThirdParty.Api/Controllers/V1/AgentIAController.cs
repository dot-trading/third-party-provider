using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Enums;
using TradingProject.ThirdParty.Domain.Models.Ai;

namespace TradingProject.ThirdParty.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AgentIAController(IAgentIAService agentIAService) : ControllerBase
{
    /// <summary>
    /// Invokes an AI service (Gemini or Grok) with the specified plan.
    /// ServiceType: <c>gemini</c> or <c>grok</c> (case-insensitive).
    /// PlanType: <c>free</c> or <c>paid</c> (case-insensitive).
    /// </summary>
    /// <example>
    /// POST /api/v1/AgentIA/gemini/free
    /// POST /api/v1/AgentIA/gemini/paid
    /// POST /api/v1/AgentIA/grok/paid
    /// POST /api/v1/AgentIA/fallback
    /// </example>
    [HttpPost("{serviceType}/{planType}")]
    public async Task<ActionResult<AiResponseDto>> Invoke(
        ServiceType serviceType,
        PlanType planType,
        [FromBody] AiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await agentIAService.GenerateContentAsync(
            request.Prompt,
            serviceType,
            planType,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Tries Gemini Free first. If it fails, automatically falls back to Gemini Paid.
    /// </summary>
    /// <example>POST /api/v1/AgentIA/fallback</example>
    [HttpPost("fallback")]
    public async Task<ActionResult<AiResponseDto>> InvokeWithFallback(
        [FromBody] AiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await agentIAService.GenerateContentWithGeminiFallbackAsync(
            request.Prompt,
            cancellationToken);

        return Ok(result);
    }
}
