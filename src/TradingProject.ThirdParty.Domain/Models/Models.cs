namespace TradingProject.ThirdParty.Domain.Models;

public record Balance(string Asset, double Free, double Locked);
public record TickerPrice(string Symbol, double Price);
public record Kline(long OpenTime, double Open, double High, double Low, double Close, double Volume);
public record Ticker24h(string Symbol, double Price, double PriceChangePercent, double QuoteVolume, double HighPrice, double LowPrice);
public record OrderResult(string OrderId, double ExecutedQty, double CummulativeQuoteQty, double Price);
