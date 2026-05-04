using System.Text.Json;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class AlternativeMeService(IHttpClientFactory httpClientFactory) : ISentimentService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("AlternativeMe");

    public async Task<FearAndGreedIndex> GetFearAndGreedIndexAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("fng/", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var data = doc.RootElement.GetProperty("data")[0];

        return new FearAndGreedIndex(
            Value: int.Parse(data.GetProperty("value").GetString()!),
            Classification: data.GetProperty("value_classification").GetString()!,
            Timestamp: long.Parse(data.GetProperty("timestamp").GetString()!)
        );
    }
}
