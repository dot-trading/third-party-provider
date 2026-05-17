using System.Text.Json.Serialization;

namespace TradingProject.ThirdParty.Application.Common.Models;

public record FearAndGreedResponseDto(List<FearAndGreedDataDto> Data);
public record FearAndGreedDataDto(
    int Value,
    [property: JsonPropertyName("value_classification")] string ValueClassification,
    long Timestamp);