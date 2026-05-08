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
    /// Gets the current ticker price for a trading pair.
    /// Corresponds to <c>GET /api/v1/Binance/price/{symbol}</c>.
    /// </summary>
    /// <param name="symbol">Trading pair symbol (e.g. "BTCUSDT").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The price response, or <c>null</c> if the symbol is not found.</returns>
    Task<BinancePriceResponse?> GetPriceAsync(string symbol, CancellationToken cancellationToken = default);
}
