using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class AlternativeMeService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions) : ISentimentService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("AlternativeMe");

    public async Task<FearAndGreedIndex> GetFearAndGreedIndexAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("fng/", cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await JsonSerializer.DeserializeAsync<FearAndGreedResponseDto>(
            await response.Content.ReadAsStreamAsync(cancellationToken), jsonOptions, cancellationToken);

        var data = dto!.Data[0];
        return new FearAndGreedIndex(
            Value: data.Value,
            Classification: data.ValueClassification,
            Timestamp: data.Timestamp);
    }

    private record FearAndGreedResponseDto(List<FearAndGreedDataDto> Data);
    private record FearAndGreedDataDto(int Value, string ValueClassification, long Timestamp);
}
