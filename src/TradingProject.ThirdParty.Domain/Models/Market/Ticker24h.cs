namespace TradingProject.ThirdParty.Domain.Models.Market;

public record Ticker24h(string Symbol, double Price, double PriceChangePercent, double QuoteVolume, double HighPrice, double LowPrice);