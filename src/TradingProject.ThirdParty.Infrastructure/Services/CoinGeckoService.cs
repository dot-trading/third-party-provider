using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class CoinGeckoService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions) : ICoinGeckoService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("CoinGecko");

    public async Task<double> GetPriceAsync(string coinId, string vsCurrency = "usd", CancellationToken cancellationToken = default)
    {
        var prices = await GetPricesAsync([coinId], vsCurrency, cancellationToken);
        return prices.TryGetValue(coinId, out var price) ? price : 0;
    }

    public async Task<Dictionary<string, double>> GetPricesAsync(IEnumerable<string> coinIds, string vsCurrency = "usd", CancellationToken cancellationToken = default)
    {
        var ids = string.Join(",", coinIds.Select(id => id.ToLower()));
        var currency = vsCurrency.ToLower();
        var url = $"simple/price?ids={ids}&vs_currencies={currency}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var data = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, double>>>(
            await response.Content.ReadAsStreamAsync(cancellationToken), jsonOptions, cancellationToken);

        return data!
            .Where(kv => kv.Value.ContainsKey(currency))
            .ToDictionary(kv => kv.Key, kv => kv.Value[currency]);
    }
}
