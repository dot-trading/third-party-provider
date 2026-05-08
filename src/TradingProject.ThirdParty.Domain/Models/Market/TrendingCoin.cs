namespace TradingProject.ThirdParty.Domain.Models.Market;

/// <summary>
/// A cryptocurrency trending on CoinGecko by search volume over the last 24 hours.
/// </summary>
public record TrendingCoin(
    string Id,
    string Name,
    string Symbol,
    int MarketCapRank,
    double PriceChangePercentage24h);
