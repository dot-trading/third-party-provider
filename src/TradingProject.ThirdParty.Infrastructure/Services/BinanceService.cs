using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Domain.Abstractions;
using TradingProject.ThirdParty.Domain.Models;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class BinanceService(IHttpClientFactory httpClientFactory, IOptions<BinanceSettings> settings) : IBinanceService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Binance");

    public async Task<Dictionary<string, double>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = settings.Value.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
            return [];

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var query = $"timestamp={timestamp}";
        var signature = Sign(query, settings.Value.ApiSecret);
        var url = $"/api/v3/account?{query}&signature={signature}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-MBX-APIKEY", apiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return doc.RootElement.GetProperty("balances")
            .EnumerateArray()
            .Where(b => double.TryParse(b.GetProperty("free").GetString(), out var free) && free > 0 ||
                        double.TryParse(b.GetProperty("locked").GetString(), out var locked) && locked > 0)
            .ToDictionary(
                b => b.GetProperty("asset").GetString()!,
                b => double.Parse(b.GetProperty("free").GetString()!));
    }

    public async Task<double> GetCurrentPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/ticker/price?symbol={symbol}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return double.Parse(doc.RootElement.GetProperty("price").GetString()!);
    }

    public async Task<List<Kline>> GetKlinesAsync(string symbol, string interval = "1h", int limit = 24, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return doc.RootElement.EnumerateArray().Select(k =>
        {
            var arr = k.EnumerateArray().ToArray();
            return new Kline(
                OpenTime: arr[0].GetInt64(),
                Open: double.Parse(arr[1].GetString()!),
                High: double.Parse(arr[2].GetString()!),
                Low: double.Parse(arr[3].GetString()!),
                Close: double.Parse(arr[4].GetString()!),
                Volume: double.Parse(arr[5].GetString()!));
        }).ToList();
    }

    public async Task<Ticker24h?> GetTicker24hAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/ticker/24hr?symbol={symbol}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = doc.RootElement;
        return new Ticker24h(
            Symbol: root.GetProperty("symbol").GetString()!,
            Price: double.Parse(root.GetProperty("lastPrice").GetString()!),
            PriceChangePercent: double.Parse(root.GetProperty("priceChangePercent").GetString()!),
            QuoteVolume: double.Parse(root.GetProperty("quoteVolume").GetString()!),
            HighPrice: double.Parse(root.GetProperty("highPrice").GetString()!),
            LowPrice: double.Parse(root.GetProperty("lowPrice").GetString()!));
    }

    private string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
