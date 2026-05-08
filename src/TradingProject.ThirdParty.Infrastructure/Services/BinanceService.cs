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
    private readonly ICacheService _cacheService;

    public BinanceService(
        IHttpClientFactory httpClientFactory,
        ITimerService timerService,
        IOptions<BinanceSettings> settings,
        JsonSerializerOptions jsonOptions,
        ICacheService cacheService)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.Binance);
        _jsonOptions = jsonOptions;

        _apiSecret = settings.Value.ApiSecret;
        _timerService = timerService;
        _cacheService = cacheService;

        if (string.IsNullOrWhiteSpace(settings.Value.ApiKey))
            throw new InvalidOperationException("cannot use binance service without a valid api key");

        if (string.IsNullOrWhiteSpace(_apiSecret))
            throw new InvalidOperationException("cannot use binance service without a valid api secret");
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

    public async Task<KLineDto[]> GetKLinesAsync(string symbol, string interval = "1h", int limit = 24,
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
            return new KLineDto(
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
            throw new InvalidOperationException("Failed to deserialize Binance order response: the response body was empty or malformed.");

        // MARKET orders always return price = 0 — there is no fixed price since the order fills
        // at whatever the order book offers, potentially across multiple positions.
        // The actual average execution price is derived by dividing the total quote asset spent
        // (CumulativeQuoteQty, e.g. USDT) by the quantity filled (ExecutedQty, e.g. BTC).
        // Example: spent 999.87 USDT to receive 0.02 BTC → avgPrice = 999.87 / 0.02 = 49 993.5 USDT/BTC.
        // Guard against division by zero in case the order was not filled at all.
        //
        // Binance always returns cummulativeQuoteQty (double-m, the original field).
        // cumulativeQuoteQty (single-m) is a later alias that may not be present on all endpoints.
        // Use the double-m field and fall back to single-m to be safe.
        var cumulativeQty = binanceOrderDto.CummulativeQuoteQty > 0
            ? binanceOrderDto.CummulativeQuoteQty
            : binanceOrderDto.CumulativeQuoteQty;
        var avgPrice = binanceOrderDto.ExecutedQty > 0 ? cumulativeQty / binanceOrderDto.ExecutedQty : 0;

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
        var stepSizeObject = await GetLotStepSizeAsync(symbol, cancellationToken);

        if (stepSizeObject is null || stepSizeObject.StepSizeValue is null or <= 0)
        {
            throw new InvalidOperationException($"Cannot place order: LotStepSize missing or invalid for {symbol}");
        }

        var stepSizeValue = stepSizeObject.StepSizeValue.Value;

        // Align quantity to LOT_SIZE step size by truncating down
        var alignedQty = Math.Truncate(quantity / stepSizeValue) * stepSizeValue;

        // Guard against zero-quantity orders (quantity smaller than one step)
        if (alignedQty <= 0)
        {
            throw new InvalidOperationException(
                $"Cannot place order for {symbol}: quantity {quantity} after LOT_SIZE alignment is zero or negative. " +
                $"The quantity is below the minimum step size of {stepSizeValue}.");
        }

        // Validate against MIN_NOTIONAL filter
        var minNotionalObject = await GetMinNotionalAsync(symbol, cancellationToken);
        var minNotional = minNotionalObject?.NotionalValue;
        if (minNotional.HasValue && minNotional.Value > 0)
        {
            var priceDto = await GetCurrentPriceAsync(symbol, cancellationToken);
            if (priceDto?.Price > 0)
            {
                var orderValue = alignedQty * priceDto.Price;
                if (orderValue < minNotional.Value)
                {
                    throw new InvalidOperationException(
                        $"Cannot place order for {symbol}: order value {orderValue} is below the minimum notional of {minNotional.Value}.");
                }
            }
        }

        // Calculate decimal places from step size (e.g., stepSize=0.001 → decimals=3)
        var decimals = Math.Max(0, (int)Math.Round(-Math.Log10(stepSizeValue)));
        var qty = alignedQty.ToString($"F{decimals}", System.Globalization.CultureInfo.InvariantCulture);

        var timestamp = _timerService.BinanceNowDateTimeOffset();
        var encodedSymbol = Uri.EscapeDataString(symbol);
        var query = $"symbol={encodedSymbol}&side=SELL&type=MARKET&quantity={qty}&newOrderRespType=RESULT&recvWindow=5000&timestamp={timestamp}";
        return await PlaceOrderAsync(query, cancellationToken);
    }

    public async Task<BinanceExchangeInfoDto?> GetExchangeInfoAsync(string symbol, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Binance.ExchangeInfo(symbol);
        var cachedData = await _cacheService.GetAsync(cacheKey, cancellationToken);

        if (cachedData is not null)
        {
            return JsonSerializer.Deserialize<BinanceExchangeInfoDto>(cachedData, _jsonOptions);
        }

        var url = $"/api/v3/exchangeInfo?symbol={symbol}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        await _cacheService.SetAsync(cacheKey, content, CacheKeys.Binance.ExchangeInfoDuration, cancellationToken);

        return JsonSerializer.Deserialize<BinanceExchangeInfoDto>(content, _jsonOptions);
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
    /// <returns>The step size, or <c>null</c> if the filter or exchange info could not be retrieved.</returns>
    public async Task<BinanceFilterDto?> GetLotStepSizeAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var exchangeInfo = await GetExchangeInfoAsync(symbol, cancellationToken);
        return exchangeInfo?.LotStepSize();
    }

    public async Task<BinanceFilterDto?> GetMinNotionalAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var exchangeInfo = await GetExchangeInfoAsync(symbol, cancellationToken);
        return exchangeInfo?.MinNotional();
    }

    private static string Sign(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
