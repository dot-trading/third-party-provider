using TradingProject.ThirdParty.Application.Common.Models;
using TradingProject.ThirdParty.Domain.Models.Market;

namespace TradingProject.ThirdParty.Application.Abstractions;

public interface ICoinGeckoService
{
    Task<CoinPriceDto> GetPriceAsync(string coinId, string vsCurrency = "usd", CancellationToken cancellationToken = default);
    Task<CoinPriceDto[]> GetPricesAsync(IEnumerable<string> coinIds, string vsCurrency = "usd", CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregated global market data: BTC/ETH dominance, total market cap,
    /// 24h volume, and market cap change percentage over the last 24 hours.
    /// Backed by the CoinGecko <c>/global</c> endpoint (1 call per cycle).
    /// </summary>
    Task<GlobalMarketData?> GetGlobalDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the top trending coins on CoinGecko by search volume over the last 24 hours.
    /// Backed by the CoinGecko <c>/search/trending</c> endpoint (max 7 coins).
    /// </summary>
    Task<TrendingCoin[]> GetTrendingCoinsAsync(CancellationToken cancellationToken = default);
}
