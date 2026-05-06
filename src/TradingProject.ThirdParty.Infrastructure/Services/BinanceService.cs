using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TradingProject.ThirdParty.Application.Abstractions;
using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Constants;
using TradingProject.ThirdParty.Infrastructure.Settings;

namespace TradingProject.ThirdParty.Infrastructure.Services;

public class BinanceService : IBinanceService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _apiSecret;
    private readonly ITimerService _timerService;

    public BinanceService(
        IHttpClientFactory httpClientFactory,
        ITimerService timerService,
        IOptions<BinanceSettings> settings,
        JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.Binance);
        _jsonOptions = jsonOptions;

        _apiSecret = settings.Value.ApiSecret;
        _timerService = timerService;

        if (string.IsNullOrWhiteSpace(settings.Value.ApiKey))
            throw new AccessViolationException("cannot use binance service without a valid api key");

        if (string.IsNullOrWhiteSpace(_apiSecret))
            throw new AccessViolationException("cannot use binance service without a valid api secret");
    }

    public async Task<ListBinanceBalanceDto?> GetBalancesAsync(
        CancellationToken cancellationToken = default)
    {
        var query = $"timestamp={_timerService.BinanceNowDateTimeOffset()}";
        var signature = Sign(
            query,
            _apiSecret);
        var url = $"/api/v3/account?{query}&signature={signature}";

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            url);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await JsonSerializer.DeserializeAsync<ListBinanceBalanceDto>(
            await response.Content.ReadAsStreamAsync(cancellationToken), _jsonOptions, cancellationToken);
    }

    public async Task<BinancePriceDto?> GetCurrentPriceAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/ticker/price?symbol={symbol}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await JsonSerializer.DeserializeAsync<BinancePriceDto>(
            await response.Content.ReadAsStreamAsync(cancellationToken), _jsonOptions, cancellationToken);
    }

    public async Task<KLine[]> GetKLinesAsync(string symbol, string interval = "1h", int limit = 24,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Klines are mixed-type arrays [[timestamp, "open", "high", ...]] — no clean typed deserialization
        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var dataArray = doc.RootElement.EnumerateArray().Select(k =>
        {
            var arr = k.EnumerateArray().ToArray();
            return new KLine(
                OpenTime: arr[0].GetInt64(),
                Open: double.Parse(arr[1].GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                High: double.Parse(arr[2].GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                Low: double.Parse(arr[3].GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                Close: double.Parse(arr[4].GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                Volume: double.Parse(arr[5].GetString()!, System.Globalization.CultureInfo.InvariantCulture));
        });
        return [.. dataArray];
    }

    public async Task<BinanceTicker24HDto?> GetTicker24HAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/ticker/24hr?symbol={symbol}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        return await JsonSerializer.DeserializeAsync<BinanceTicker24HDto>(
            await response.Content.ReadAsStreamAsync(cancellationToken), _jsonOptions, cancellationToken);
    }

    public async Task<BinanceOrderDto> PlaceOrderAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var signature = Sign(query, _apiSecret);
        var body = new StringContent(
            $"{query}&signature={signature}",
            Encoding.UTF8,
            "application/x-www-form-urlencoded");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/v3/order")
        {
            Content = body
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Binance order failed ({(int)response.StatusCode}): {errorBody}");
        }

        var binanceOrderDto = await JsonSerializer.DeserializeAsync<BinanceOrderDto>(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            _jsonOptions,
            cancellationToken);

        if (binanceOrderDto is null)
            throw new ArgumentNullException(nameof(binanceOrderDto));

        // MARKET orders always return price = 0 — there is no fixed price since the order fills
        // at whatever the order book offers, potentially across multiple positions.
        // The actual average execution price is derived by dividing the total quote asset spent
        // (CumulativeQuoteQty, e.g. USDT) by the quantity filled (ExecutedQty, e.g. BTC).
        // Example: spent 999.87 USDT to receive 0.02 BTC → avgPrice = 999.87 / 0.02 = 49 993.5 USDT/BTC.
        // Guard against division by zero in case the order was not filled at all.
        //
        // Note on CumulativeQuoteQty vs CummulativeQuoteQty:
        // Binance shipped the original field with a typo ("cummulative", double-m) and cannot remove
        // it without breaking existing integrations. "cumulativeQuoteQty" (single-m) was added later
        // as a correctly-spelled alias — both fields carry the same value.
        var avgPrice =
            binanceOrderDto.ExecutedQty > 0 ?
                binanceOrderDto.CumulativeQuoteQty / binanceOrderDto.ExecutedQty
                : 0;

        return binanceOrderDto with { Price = avgPrice };
    }

    public async Task<BinanceOrderDto> PlaceMarketBuyAsync(
        string symbol,
        double quoteOrderQty,
        CancellationToken cancellationToken = default)
    {
        var qty = quoteOrderQty.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
        var query =
            $"symbol={symbol}&side=BUY&type=MARKET&quoteOrderQty={qty}&timestamp={_timerService.BinanceNowDateTimeOffset()}";
        return await PlaceOrderAsync(query, cancellationToken);
    }

    public async Task<BinanceOrderDto> PlaceMarketSellAsync(string symbol, double quantity,
        CancellationToken cancellationToken = default)
    {
        var stepSize = await GetLotStepSizeAsync(symbol, cancellationToken);
        var alignedQty = stepSize > 0 ? Math.Truncate(quantity / stepSize) * stepSize : quantity;
        var decimals = stepSize > 0 ? Math.Max(0, -(int)Math.Floor(Math.Log10(stepSize))) : 8;
        var qty = alignedQty.ToString($"F{decimals}", System.Globalization.CultureInfo.InvariantCulture);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var query = $"symbol={symbol}&side=SELL&type=MARKET&quantity={qty}&timestamp={timestamp}";
        return await PlaceOrderAsync(query, cancellationToken);
    }
    
    public async Task<BinanceExchangeInfoDto?> GetExchangeInfoAsync(string symbol, CancellationToken cancellationToken)
    {
        var url = $"/api/v3/exchangeInfo?symbol={symbol}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        return await JsonSerializer.DeserializeAsync<BinanceExchangeInfoDto>(
            await response.Content.ReadAsStreamAsync(cancellationToken), _jsonOptions, cancellationToken);
    }

    /// <summary>
    /// Returns the LOT_SIZE step size for <paramref name="symbol"/>.
    /// <para>
    /// Binance enforces a <c>LOT_SIZE</c> filter on every trading pair that defines the minimum
    /// quantity increment an order quantity must be a multiple of. For example, a step size of
    /// <c>0.001</c> means valid quantities are 0.001, 0.002, 0.003, … — any other value is
    /// rejected with error code -1013.
    /// </para>
    /// <para>
    /// This value is used by <see cref="PlaceMarketSellAsync"/> to truncate the sell quantity to
    /// the nearest valid multiple before submitting the order.
    /// </para>
    /// </summary>
    /// <returns>The step size, or <c>0</c> if the exchange info could not be retrieved.</returns>
    private async Task<double> GetLotStepSizeAsync(string symbol, CancellationToken cancellationToken)
    {
        var exchangeInfo = await GetExchangeInfoAsync(symbol, cancellationToken);
        return exchangeInfo?.Symbols[0].Filters
            .Where(f => f.FilterType == "LOT_SIZE")
            .Select(f => f.StepSize ?? 0)
            .FirstOrDefault() ?? 0;
    }

    public async Task<double> GetMinNotionalAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var exchangeInfo = await GetExchangeInfoAsync(symbol, cancellationToken);
        return exchangeInfo?.Symbols[0].Filters
            .Where(f => f.FilterType is "NOTIONAL" or "MIN_NOTIONAL")
            .Select(f => f.MinNotional ?? f.Notional ?? 0)
            .FirstOrDefault() ?? 0;
    }

    


    private static string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}