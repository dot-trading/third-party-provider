using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class CoinGeckoService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions) : ICoinGeckoService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("CoinGecko");

    public async Task<CoinPriceDto> GetPriceAsync(string coinId, string vsCurrency = "usd", CancellationToken cancellationToken = default)
    {
        var prices = await GetPricesAsync([coinId], vsCurrency, cancellationToken);
        return prices.FirstOrDefault() ?? new CoinPriceDto(coinId, 0);
    }

    public async Task<CoinPriceDto[]> GetPricesAsync(IEnumerable<string> coinIds, string vsCurrency = "usd", CancellationToken cancellationToken = default)
    {
        var ids = string.Join(",", coinIds.Select(id => id.ToLower()));
        var currency = vsCurrency.ToLower();
        var url = $"simple/price?ids={ids}&vs_currencies={currency}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var data = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, double>>>(
            await response.Content.ReadAsStreamAsync(cancellationToken), jsonOptions, cancellationToken);

        if (data is null) return [];

        return data
            .Where(kv => kv.Value.ContainsKey(currency))
            .Select(kv => new CoinPriceDto(kv.Key, kv.Value[currency]))
            .ToArray();
    }
}
