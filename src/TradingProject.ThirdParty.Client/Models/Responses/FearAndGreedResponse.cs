using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Client.Models.Responses;

/// <summary>
/// Response returned by <c>GET /api/v{version}/MarketData/sentiment/fear-and-greed</c> (V1+).
/// Contains the current Fear &amp; Greed Index value from alternative.me.
/// </summary>
/// <param name="Value">The Fear &amp; Greed Index value (0–100).</param>
/// <param name="Classification">The classification label (e.g. "Extreme Fear", "Fear", "Neutral", "Greed", "Extreme Greed").</param>
/// <param name="Timestamp">Unix timestamp (seconds) when the index was recorded.</param>
public record FearAndGreedResponse(
    [property: JsonPropertyName("value")] int Value,
    [property: JsonPropertyName("classification")] string Classification,
    [property: JsonPropertyName("timestamp")] long Timestamp
);
