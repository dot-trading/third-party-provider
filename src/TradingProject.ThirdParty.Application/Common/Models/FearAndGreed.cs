using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Application.Common.Models;

public record FearAndGreedResponseDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("data")] FearAndGreedDataDto[] Data);

public record FearAndGreedDataDto(
    [property: JsonPropertyName("value")] int Value,
    [property: JsonPropertyName("value_classification")] string ValueClassification,
    [property: JsonPropertyName("timestamp")] long Timestamp,
    [property: JsonPropertyName("time_until_update")] long TimeUntilUpdate);