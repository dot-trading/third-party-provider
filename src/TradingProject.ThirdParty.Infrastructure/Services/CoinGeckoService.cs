using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class CoinGeckoService(IHttpClientFactory httpClientFactory, IOptions<CoinGeckoSettings> settings) : ICoinGeckoService
{
    private readonly HttpClient _httpClient = CreateClient(httpClientFactory, settings.Value);

    private static HttpClient CreateClient(IHttpClientFactory factory, CoinGeckoSettings settings)
    {
        var client = factory.CreateClient("CoinGecko");
        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            // For demo/free plan, the header is x-cg-demo-api-key
            // For pro plan, it's x-cg-pro-api-key
            // We'll use the demo one as it's more likely for the free plan mentioned
            client.DefaultRequestHeaders.Add("x-cg-demo-api-key", settings.ApiKey);
        }
        return client;
    }

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
