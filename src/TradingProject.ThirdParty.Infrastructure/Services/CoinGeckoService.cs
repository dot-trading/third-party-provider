using System.Text.Json;
using TradingProject.ThirdParty.Domain.Abstractions;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class CoinGeckoService(IHttpClientFactory httpClientFactory) : ICoinGeckoService
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
        var url = $"simple/price?ids={ids}&vs_currencies={vsCurrency.ToLower()}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var result = new Dictionary<string, double>();

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            if (property.Value.TryGetProperty(vsCurrency.ToLower(), out var priceElement))
            {
                result[property.Name] = priceElement.GetDouble();
            }
        }

        return result;
    }
}
