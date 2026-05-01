using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, double> _lotStepCache = new();
    private readonly ConcurrentDictionary<string, double> _minNotionalCache = new();

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
            .Select(b => new
            {
                Asset = b.GetProperty("asset").GetString()!,
                Free = double.TryParse(b.GetProperty("free").GetString(), out var f) ? f : 0,
                Locked = double.TryParse(b.GetProperty("locked").GetString(), out var l) ? l : 0
            })
            .Where(b => b.Free + b.Locked > 0)
            .ToDictionary(b => b.Asset, b => b.Free + b.Locked);
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

    public async Task<OrderResult> PlaceMarketBuyAsync(string symbol, double quoteOrderQty, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var qty = quoteOrderQty.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
        var query = $"symbol={symbol}&side=BUY&type=MARKET&quoteOrderQty={qty}&timestamp={timestamp}";
        return await PlaceOrderAsync(query, cancellationToken);
    }

    public async Task<OrderResult> PlaceMarketSellAsync(string symbol, double quantity, CancellationToken cancellationToken = default)
    {
        var stepSize = await GetLotStepSizeAsync(symbol, cancellationToken);
        var alignedQty = stepSize > 0 ? Math.Truncate(quantity / stepSize) * stepSize : quantity;
        var decimals = stepSize > 0 ? Math.Max(0, -(int)Math.Floor(Math.Log10(stepSize))) : 8;
        var qty = alignedQty.ToString($"F{decimals}", System.Globalization.CultureInfo.InvariantCulture);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var query = $"symbol={symbol}&side=SELL&type=MARKET&quantity={qty}&timestamp={timestamp}";
        return await PlaceOrderAsync(query, cancellationToken);
    }

    private async Task<double> GetLotStepSizeAsync(string symbol, CancellationToken cancellationToken)
    {
        if (_lotStepCache.TryGetValue(symbol, out var cached))
            return cached;

        var url = $"/api/v3/exchangeInfo?symbol={symbol}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return 0;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var stepSize = doc.RootElement
            .GetProperty("symbols")[0]
            .GetProperty("filters")
            .EnumerateArray()
            .Where(f => f.GetProperty("filterType").GetString() == "LOT_SIZE")
            .Select(f => double.Parse(f.GetProperty("stepSize").GetString()!, System.Globalization.CultureInfo.InvariantCulture))
            .FirstOrDefault();

        _lotStepCache[symbol] = stepSize;
        return stepSize;
    }

    public async Task<double> GetMinNotionalAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (_minNotionalCache.TryGetValue(symbol, out var cached))
            return cached;

        var url = $"/api/v3/exchangeInfo?symbol={symbol}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return 0;

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var minNotional = doc.RootElement
            .GetProperty("symbols")[0]
            .GetProperty("filters")
            .EnumerateArray()
            .Where(f => f.GetProperty("filterType").GetString() == "NOTIONAL" || f.GetProperty("filterType").GetString() == "MIN_NOTIONAL")
            .Select(f => {
                if (f.TryGetProperty("minNotional", out var mn))
                    return double.Parse(mn.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                if (f.TryGetProperty("notional", out var n))
                    return double.Parse(n.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
                return 0.0;
            })
            .FirstOrDefault();

        _minNotionalCache[symbol] = minNotional;
        return minNotional;
    }

    private async Task<OrderResult> PlaceOrderAsync(string query, CancellationToken cancellationToken)
    {
        var signature = Sign(query, settings.Value.ApiSecret);
        var body = new StringContent($"{query}&signature={signature}", Encoding.UTF8, "application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v3/order") { Content = body };
        request.Headers.Add("X-MBX-APIKEY", settings.Value.ApiKey);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Binance order failed ({(int)response.StatusCode}): {errorBody}");
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var root = doc.RootElement;

        var executedQty = double.Parse(root.GetProperty("executedQty").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var cummulativeQuoteQty = double.Parse(root.GetProperty("cummulativeQuoteQty").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        var avgPrice = executedQty > 0 ? cummulativeQuoteQty / executedQty : 0;

        return new OrderResult(
            OrderId: root.GetProperty("orderId").GetInt64().ToString(),
            ExecutedQty: executedQty,
            CummulativeQuoteQty: cummulativeQuoteQty,
            Price: avgPrice);
    }

    private string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
