using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradingProject.ThirdParty.Client.Models.Responses;

namespace TradingProject.ThirdParty.Client.Services;

/// <summary>
/// Default <see cref="IThirdPartyApiClient"/> implementation backed by <see cref="System.Net.Http.HttpClient"/>.
/// Communicates with the Third-Party Provider API using the V1 route prefix.
/// </summary>
public class ThirdPartyApiClient : IThirdPartyApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThirdPartyApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="ThirdPartyApiClient"/>.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> configured by DI.</param>
    /// <param name="logger">Logger instance.</param>
    public ThirdPartyApiClient(HttpClient httpClient, ILogger<ThirdPartyApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ListBinanceBalanceResponse?> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching balances from ThirdParty API (V1)");

            var response = await _httpClient.GetAsync("api/v1/Binance/balances", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<ListBinanceBalanceResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully retrieved {BalanceCount} balances", result?.Balances.Length ?? 0);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch balances from ThirdParty API");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ListBinanceBalanceResponse?> GetBalancesAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        try
        {
            _logger.LogDebug("Fetching balances for symbol {Symbol} from ThirdParty API (V1)", symbol);

            var response = await _httpClient.GetAsync($"api/v1/Binance/balances/{symbol}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Balances not found for symbol {Symbol}", symbol);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<ListBinanceBalanceResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully retrieved {BalanceCount} balances for symbol {Symbol}", result?.Balances.Length ?? 0, symbol);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch balances for symbol {Symbol} from ThirdParty API", symbol);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BinancePriceResponse?> GetPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        try
        {
            _logger.LogDebug("Fetching price for symbol {Symbol} from ThirdParty API (V1)", symbol);

            var response = await _httpClient.GetAsync($"api/v1/Binance/price/{symbol}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<BinancePriceResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully retrieved price {Price} for symbol {Symbol}", result?.Price, symbol);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch price for symbol {Symbol} from ThirdParty API", symbol);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BinanceNotionalResponse?> GetMinNotionalAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        try
        {
            _logger.LogDebug("Fetching min notional for symbol {Symbol} from ThirdParty API (V1)", symbol);

            var response = await _httpClient.GetAsync($"api/v1/Binance/notional/{symbol}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<BinanceNotionalResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully retrieved min notional for symbol {Symbol}", symbol);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch min notional for symbol {Symbol} from ThirdParty API", symbol);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BinanceKLineResponse[]?> GetKlinesAsync(
        string symbol,
        string interval = "1h",
        int limit = 24,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(interval);

        try
        {
            _logger.LogDebug("Fetching klines for symbol {Symbol}, interval {Interval}, limit {Limit} from ThirdParty API (V1)", symbol, interval, limit);

            var url = $"api/v1/Binance/klines/{symbol}?interval={Uri.EscapeDataString(interval)}&limit={limit}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<BinanceKLineResponse[]>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully retrieved {Count} klines for symbol {Symbol}", result?.Length ?? 0, symbol);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch klines for symbol {Symbol} from ThirdParty API", symbol);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BinanceTicker24HResponse?> GetTicker24hAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        try
        {
            _logger.LogDebug("Fetching 24h ticker for symbol {Symbol} from ThirdParty API (V1)", symbol);

            var response = await _httpClient.GetAsync($"api/v1/Binance/ticker/{symbol}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("24h ticker not found for symbol {Symbol}", symbol);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<BinanceTicker24HResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully retrieved 24h ticker for symbol {Symbol}: price={Price}", symbol, result?.Price);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch 24h ticker for symbol {Symbol} from ThirdParty API", symbol);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BinanceOrderResultResponse?> PlaceMarketBuyAsync(PlaceMarketBuyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogDebug("Placing market buy order for symbol {Symbol} with quoteQty {QuoteQty} from ThirdParty API (V1)",
                request.Symbol, request.QuoteOrderQty);

            var response = await _httpClient.PostAsJsonAsync("api/v1/Binance/order/buy", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Binance order failed ({(int)response.StatusCode}): {errorBody}");
            }

            var result = await response.Content
                .ReadFromJsonAsync<BinanceOrderResultResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully placed market buy order for symbol {Symbol}: orderId={OrderId}",
                request.Symbol, result?.OrderId);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to place market buy order for symbol {Symbol} from ThirdParty API", request.Symbol);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<BinanceOrderResultResponse?> PlaceMarketSellAsync(PlaceMarketSellRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogDebug("Placing market sell order for symbol {Symbol} with quantity {Quantity} from ThirdParty API (V1)",
                request.Symbol, request.Quantity);

            var response = await _httpClient.PostAsJsonAsync("api/v1/Binance/order/sell", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Binance order failed ({(int)response.StatusCode}): {errorBody}");
            }

            var result = await response.Content
                .ReadFromJsonAsync<BinanceOrderResultResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully placed market sell order for symbol {Symbol}: orderId={OrderId}",
                request.Symbol, result?.OrderId);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to place market sell order for symbol {Symbol} from ThirdParty API", request.Symbol);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FearAndGreedResponse?> GetFearAndGreedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Fetching Fear & Greed Index from ThirdParty API (V1)");

            var response = await _httpClient.GetAsync("api/v1/market-data/sentiment/fear-and-greed", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Fear & Greed Index not found");
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<FearAndGreedResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Successfully retrieved Fear & Greed Index: Value={Value}, Classification={Classification}",
                result?.Value, result?.Classification);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch Fear & Greed Index from ThirdParty API");
            throw;
        }
    }

    // ========================================================================
    // AI / AgentIA
    // ========================================================================

    /// <inheritdoc />
    public async Task<AiResponse?> InvokeGeminiFreeAsync(AiServiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogDebug("Invoking Gemini Free via AgentIA");

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/AgentIA/gemini/free", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<AiResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Gemini Free response: IsSuccess={IsSuccess}", result?.IsSuccess);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to invoke Gemini Free");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AiResponse?> InvokeGeminiPaidAsync(AiServiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogDebug("Invoking Gemini Paid via AgentIA");

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/AgentIA/gemini/paid", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<AiResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Gemini Paid response: IsSuccess={IsSuccess}", result?.IsSuccess);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to invoke Gemini Paid");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AiResponse?> InvokeGrokAsync(AiServiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogDebug("Invoking Grok via AgentIA");

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/AgentIA/grok/paid", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<AiResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Grok response: IsSuccess={IsSuccess}", result?.IsSuccess);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to invoke Grok");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<AiResponse?> InvokeGeminiWithFallbackAsync(AiServiceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            _logger.LogDebug("Invoking Gemini with fallback (Free → Paid) via AgentIA");

            var response = await _httpClient.PostAsJsonAsync(
                "api/v1/AgentIA/fallback", request, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<AiResponse>(JsonOptions, cancellationToken);

            _logger.LogDebug("Gemini fallback response: IsSuccess={IsSuccess}, PlanType={PlanType}",
                result?.IsSuccess, result?.PlanType);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to invoke Gemini with fallback");
            throw;
        }
    }
}
