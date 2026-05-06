namespace TradingProject.ThirdParty.Application.Common.Models;


public record FearAndGreedResponseDto(List<FearAndGreedDataDto> Data);
public record FearAndGreedDataDto(int Value, string ValueClassification, long Timestamp);
public record FearAndGreedIndex(int Value, string Classification, long Timestamp);