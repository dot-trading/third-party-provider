namespace TradingProject.ThirdParty.Application.Tests.Integration;

/// <summary>
/// xUnit fact that only executes in Debug configuration.
/// Use for integration tests that make real network calls and must never run in CI.
/// </summary>
public sealed class DebugOnlyFactAttribute : FactAttribute
{
    public DebugOnlyFactAttribute()
    {
#if !DEBUG
        Skip = "Integration tests only run in Debug configuration.";
#endif
    }
}
