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

            _logger.LogDebug("Successfully retrieved {BalanceCount} balances", result?.Balances?.Length ?? 0);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch balances from ThirdParty API");
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
}
