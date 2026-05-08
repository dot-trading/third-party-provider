namespace TradingProject.ThirdParty.Domain.Models.Market;

/// <summary>
/// Aggregated global cryptocurrency market data from CoinGecko.
/// Provides macro-level context for trading decisions (e.g., BTC dominance trend).
/// </summary>
public record GlobalMarketData(
    double BtcDominance,
    double EthDominance,
    double TotalMarketCapUsd,
    double TotalVolumeUsd,
    double MarketCapChangePercentage24hUsd,
    long UpdatedAt);
