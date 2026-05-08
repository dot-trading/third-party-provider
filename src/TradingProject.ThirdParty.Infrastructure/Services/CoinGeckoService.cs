using System.Text.Json;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class CoinGeckoService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions) : ICoinGeckoService
{
    private readonly HttpClient _httpClient =
        httpClientFactory.CreateClient(HttpClientNames.CoinGecko);

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

    public async Task<GlobalMarketData?> GetGlobalDataAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("global", cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await JsonSerializer.DeserializeAsync<CoinGeckoGlobalResponseDto>(
            await response.Content.ReadAsStreamAsync(cancellationToken), jsonOptions, cancellationToken);

        if (dto?.Data is null) return null;

        var d = dto.Data;
        return new GlobalMarketData(
            BtcDominance: d.MarketCapPercentage.GetValueOrDefault("btc"),
            EthDominance: d.MarketCapPercentage.GetValueOrDefault("eth"),
            TotalMarketCapUsd: d.TotalMarketCap.GetValueOrDefault("usd"),
            TotalVolumeUsd: d.TotalVolume.GetValueOrDefault("usd"),
            MarketCapChangePercentage24hUsd: d.MarketCapChangePercentage24hUsd,
            UpdatedAt: d.UpdatedAt);
    }

    public async Task<TrendingCoin[]> GetTrendingCoinsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("search/trending", cancellationToken);
        response.EnsureSuccessStatusCode();

        var dto = await JsonSerializer.DeserializeAsync<CoinGeckoTrendingResponseDto>(
            await response.Content.ReadAsStreamAsync(cancellationToken), jsonOptions, cancellationToken);

        if (dto?.Coins is null) return [];

        return dto.Coins
            .Select(c => new TrendingCoin(
                Id: c.Item.Id,
                Name: c.Item.Name,
                Symbol: c.Item.Symbol,
                MarketCapRank: c.Item.MarketCapRank,
                PriceChangePercentage24h: c.Item.Data?.PriceChangePercentage24h?.GetValueOrDefault("usd") ?? 0))
            .ToArray();
    }
}
