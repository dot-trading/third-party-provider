using TradingProject.ThirdParty.Client.Models.Responses;

namespace TradingProject.ThirdParty.Client.Services;

/// <summary>
/// Typed client for consuming the Third-Party Provider API (V1+).
/// All methods target the <c>/api/v1</c> route prefix.
/// </summary>
public interface IThirdPartyApiClient
{
    /// <summary>
    /// Retrieves the current wallet balances from the Binance account.
    /// Corresponds to <c>GET /api/v1/Binance/balances</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full balance response, or <c>null</c> if the account is not found.</returns>
    Task<ListBinanceBalanceResponse?> GetBalancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves balances for the base and quote assets of a specific trading pair.
    /// Corresponds to <c>GET /api/v1/Binance/balances/{symbol}</c>.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The filtered balance response, or <c>null</c> if the symbol is not found.</returns>
    Task<ListBinanceBalanceResponse?> GetBalancesAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current ticker price for a trading pair.
    /// Corresponds to <c>GET /api/v1/Binance/price/{symbol}</c>.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The price response, or <c>null</c> if the symbol is not found.</returns>
    Task<BinancePriceResponse?> GetPriceAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the MIN_NOTIONAL filter for a trading pair.
    /// Corresponds to <c>GET /api/v1/Binance/notional/{symbol}</c>.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The min notional filter response, or <c>null</c> if the symbol is not found.</returns>
    Task<BinanceNotionalResponse?> GetMinNotionalAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves K-Line (candlestick) data for a trading pair.
    /// Corresponds to <c>GET /api/v1/Binance/klines/{symbol}?interval=...&amp;limit=...</c>.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
    /// <param name="interval">K-Line interval (e.g. "1h", "1d"). Defaults to "1h".</param>
    /// <param name="limit">Number of candles to retrieve. Defaults to 24.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An array of K-Line data points, or an empty array if no data is available.</returns>
    Task<BinanceKLineResponse[]?> GetKlinesAsync(
        string symbol,
        string interval = "1h",
        int limit = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves 24-hour ticker statistics for a trading pair.
    /// Corresponds to <c>GET /api/v1/Binance/ticker/{symbol}</c>.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ticker response, or <c>null</c> if the symbol is not found.</returns>
    Task<BinanceTicker24HResponse?> GetTicker24hAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a market buy order on Binance.
    /// Corresponds to <c>POST /api/v1/Binance/order/buy</c>.
    /// </summary>
    /// <param name="request">The buy order details (symbol and quote order quantity).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order result response, or <c>null</c> if the order could not be placed.</returns>
    Task<BinanceOrderResultResponse?> PlaceMarketBuyAsync(PlaceMarketBuyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a market sell order on Binance.
    /// Corresponds to <c>POST /api/v1/Binance/order/sell</c>.
    /// </summary>
    /// <param name="request">The sell order details (symbol and base asset quantity).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order result response, or <c>null</c> if the order could not be placed.</returns>
    Task<BinanceOrderResultResponse?> PlaceMarketSellAsync(PlaceMarketSellRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current Fear &amp; Greed Index from alternative.me.
    /// Corresponds to <c>GET /api/v1/MarketData/sentiment/fear-and-greed</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Fear &amp; Greed Index response, or <c>null</c> if data is unavailable.</returns>
    Task<FearAndGreedResponse?> GetFearAndGreedAsync(CancellationToken cancellationToken = default);

    // ========================================================================
    // AI / AgentIA
    // ========================================================================

    /// <summary>
    /// Invokes Gemini AI with the Free plan.
    /// Corresponds to <c>POST /api/v1/AgentIA/gemini/free</c>.
    /// </summary>
    Task<AiResponse?> InvokeGeminiFreeAsync(AiServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes Gemini AI with the Paid plan.
    /// Corresponds to <c>POST /api/v1/AgentIA/gemini/paid</c>.
    /// </summary>
    Task<AiResponse?> InvokeGeminiPaidAsync(AiServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes Grok AI.
    /// Corresponds to <c>POST /api/v1/AgentIA/grok/paid</c>.
    /// </summary>
    Task<AiResponse?> InvokeGrokAsync(AiServiceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes Gemini with automatic fallback (Free → Paid).
    /// Corresponds to <c>POST /api/v1/AgentIA/fallback</c>.
    /// </summary>
    Task<AiResponse?> InvokeGeminiWithFallbackAsync(AiServiceRequest request, CancellationToken cancellationToken = default);
}
